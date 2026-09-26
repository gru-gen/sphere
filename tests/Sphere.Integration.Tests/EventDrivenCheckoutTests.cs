using Sphere.Integration.Tests.Basket;
using Sphere.Integration.Tests.Catalog;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Sphere.Integration.Tests;

[Collection("postgres")]
public sealed class EventDrivenCheckoutTests : IClassFixture<KafkaFixture>, IDisposable
{
    private readonly PostgresContainer _fixture;
    private readonly CatalogServiceFactory _catalog;
    private readonly BasketServiceFactory _basket;
    private readonly OrderingServiceFactory _ordering;

    public EventDrivenCheckoutTests(PostgresContainer fixture, KafkaFixture kafka)
    {
        _fixture = fixture;
        _catalog = new CatalogServiceFactory(fixture);
        _basket = new BasketServiceFactory(fixture, kafka.BootstrapServers);
        _ordering = new OrderingServiceFactory(
            fixture, _catalog.CreateClient(), kafka.BootstrapServers);
    }

    public void Dispose()
    {
        _ordering.Dispose();
        _basket.Dispose();
        _catalog.Dispose();
    }

    private static async Task<JsonElement> PollOrderAsync(HttpClient ordering, Guid orderId)
    {
        // why: 404-then-200 IS the contract now — the test polls exactly the
        // way a client would, and fails loudly if the fact never lands.
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            var response = await ordering.GetAsync($"/api/orders/{orderId}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return await response.Content.ReadFromJsonAsync<JsonElement>();
            }
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            await Task.Delay(250);
        }
        Assert.Fail($"Order {orderId} never appeared — the fact was lost.");
        return default;
    }

    [Fact]
    public async Task The_full_loop_browse_basket_checkout_wait_read_cancel()
    {
        var catalogClient = _catalog.CreateClient();
        var basketClient = _basket.CreateClient();
        var customerId = Guid.CreateVersion7();

        // browse: the CATALOG SERVICE answers
        var browse = await catalogClient.GetFromJsonAsync<JsonElement>(
            "/api/products?pageSize=1");
        var productId = browse.GetProperty("items")[0].GetProperty("id").GetGuid();

        // basket: the BASKET SERVICE answers, and will announce the fact
        var add = await basketClient.PostAsJsonAsync($"/api/basket/{customerId}/items",
            new { productId, quantity = 2 });
        Assert.Equal(HttpStatusCode.NoContent, add.StatusCode);

        // checkout: 202 — accepted, not finished. The reply names the order.
        var checkout = await basketClient.PostAsJsonAsync("/api/checkout",
            new { customerId });
        Assert.Equal(HttpStatusCode.Accepted, checkout.StatusCode);
        var accepted = await checkout.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = accepted.GetProperty("orderId").GetGuid();

        // the basket is ALREADY empty — cleared before anyone consumed anything
        var basket = await basketClient.GetFromJsonAsync<JsonElement>(
            $"/api/basket/{customerId}");
        Assert.Equal(0, basket.GetProperty("items").GetArrayLength());

        // wait: the order arrives through the log
        var orderingClient = _ordering.CreateClient();
        var order = await PollOrderAsync(orderingClient, orderId);
        Assert.Equal("Placed", order.GetProperty("status").GetString());
        Assert.Equal(1, order.GetProperty("items").GetArrayLength());
        Assert.Equal(2, order.GetProperty("items")[0].GetProperty("quantity").GetInt32());

        // cancel: 204, then the domain says no with a 422 problem document
        var cancel = await orderingClient.PostAsync($"/api/orders/{orderId}/cancel", null);
        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);
        var again = await orderingClient.PostAsync($"/api/orders/{orderId}/cancel", null);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, again.StatusCode);
    }

    [Fact]
    public async Task Replaying_the_checkout_key_still_yields_exactly_one_order()
    {
        var basketClient = _basket.CreateClient();
        var customerId = Guid.CreateVersion7();
        var browse = await _catalog.CreateClient().GetFromJsonAsync<JsonElement>(
            "/api/products?pageSize=1");
        var productId = browse.GetProperty("items")[0].GetProperty("id").GetGuid();
        var add = await basketClient.PostAsJsonAsync($"/api/basket/{customerId}/items",
            new { productId, quantity = 1 });
        Assert.Equal(HttpStatusCode.NoContent, add.StatusCode);

        var key = $"order-{Guid.CreateVersion7()}";
        async Task<Guid> CheckoutAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/checkout")
            {
                Content = JsonContent.Create(new { customerId }),
            };
            request.Headers.Add("Idempotency-Key", key);
            var response = await basketClient.SendAsync(request);
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body.GetProperty("orderId").GetGuid();
        }

        var first = await CheckoutAsync();
        var second = await CheckoutAsync();
        Assert.Equal(first, second);

        await PollOrderAsync(_ordering.CreateClient(), first);
        await using var db = _fixture.CreateOrderingContext();
        Assert.Equal(1, await db.Orders.CountAsync(o => o.CustomerId == customerId));
    }
}

