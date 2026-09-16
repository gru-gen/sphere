using Microsoft.EntityFrameworkCore;
using Sphere.Integration.Tests.Basket;
using Sphere.Integration.Tests.Catalog;
using Sphere.Integration.Tests.Ordering;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Sphere.Integration.Tests;

[Collection("postgres")]
public sealed class EventDrivenCheckoutTests : IClassFixture<KafkaFixture>, IDisposable
{
    private readonly PostgresFixture _postgresFixture;
    private readonly CatalogServiceFactory _catalogServiceFactory;
    private readonly BasketServiceFactory _basketServiceFactory;
    private readonly OrderingServiceFactory _orderingServiceFactory;

    public EventDrivenCheckoutTests(PostgresFixture postgresFixture, KafkaFixture kafkaFixture)
    {
        _postgresFixture = postgresFixture;
        _catalogServiceFactory = new CatalogServiceFactory(_postgresFixture);
        _basketServiceFactory = new BasketServiceFactory(_postgresFixture, kafkaFixture.BootstrapServers);
        _orderingServiceFactory = new OrderingServiceFactory(_postgresFixture,
            _catalogServiceFactory.CreateClient(), kafkaFixture.BootstrapServers);
    }

    public void Dispose()
    {
        _orderingServiceFactory.Dispose();
        _basketServiceFactory.Dispose();
        _catalogServiceFactory.Dispose();
    }

    private static async Task<JsonElement> PollOrderAsync(HttpClient ordering, Guid orderId)
    {
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
        var catalogClient = _catalogServiceFactory.CreateClient();
        var basketClient = _basketServiceFactory.CreateClient();
        var orderingClient = _orderingServiceFactory.CreateClient();

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
        var order = await PollOrderAsync(orderingClient, orderId);
        Assert.Equal("Placed", order.GetProperty("status").GetString());
        Assert.Equal(1, order.GetProperty("lines").GetArrayLength());
        Assert.Equal(2, order.GetProperty("lines")[0].GetProperty("quantity").GetInt32());

        // cancel: 204, then the domain says no with a 422 problem document
        var cancel = await orderingClient.PostAsync($"/api/orders/{orderId}/cancel", null);
        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);
        var again = await orderingClient.PostAsync($"/api/orders/{orderId}/cancel", null);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, again.StatusCode);
    }

    [Fact]
    public async Task Replaying_the_checkout_key_still_yields_exactly_one_order()
    {
        var basketClient = _basketServiceFactory.CreateClient();
        var customerId = Guid.CreateVersion7();
        var browse = await _catalogServiceFactory.CreateClient().GetFromJsonAsync<JsonElement>(
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
                Content = JsonContent.Create(new { customerId })
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

        await PollOrderAsync(_orderingServiceFactory.CreateClient(), first);
        await using var db = _postgresFixture.CreateOrderingContext();
        Assert.Equal(1, await db.Orders.CountAsync(o => o.CustomerId == customerId));
    }
}
