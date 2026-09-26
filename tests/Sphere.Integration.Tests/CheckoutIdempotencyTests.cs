using Sphere.Integration.Tests.Basket;
using Sphere.Integration.Tests.Catalog;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Sphere.Integration.Tests;

// summary: the Idempotency-Key promise, proven on the three-process rig — the
// same key returns the SAME order, even after the basket is long gone.
[Collection("postgres")]
public sealed class CheckoutIdempotencyTests : IDisposable
{
    private readonly PostgresContainer _postgresContainer;
    private readonly CatalogServiceFactory _catalog;
    private readonly BasketServiceFactory _basket;
    private readonly OrderingServiceFactory _ordering;

    public CheckoutIdempotencyTests(PostgresContainer postgresContainer)
    {
        _postgresContainer = postgresContainer;
        _catalog = new CatalogServiceFactory(_postgresContainer);
        _basket = new BasketServiceFactory(_postgresContainer);
        _ordering = new OrderingServiceFactory(_postgresContainer, _catalog.CreateClient(), _basket.CreateClient());
    }

    public void Dispose()
    {
        _ordering.Dispose();
        _basket.Dispose();
        _catalog.Dispose();
    }

    private static Task<HttpResponseMessage> PostCheckoutAsync(
        HttpClient client, Guid customerId, string key)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/checkout")
        {
            Content = JsonContent.Create(new { customerId }),
        };
        request.Headers.Add("Idempotency-Key", key);
        return client.SendAsync(request);
    }

    [Fact]
    public async Task The_same_key_replays_the_same_order_even_with_an_empty_basket()
    {
        var browse = await _catalog.CreateClient().GetFromJsonAsync<JsonElement>(
            "/api/products?pageSize=1");
        var productId = browse.GetProperty("items")[0].GetProperty("id").GetGuid();

        var orderingClient = _ordering.CreateClient();
        var customerId = Guid.CreateVersion7();
        var add = await _basket.CreateClient().PostAsJsonAsync(
            $"/api/basket/{customerId}/items", new { productId, quantity = 1 });
        Assert.Equal(HttpStatusCode.NoContent, add.StatusCode);
        var key = $"order-{Guid.CreateVersion7()}";

        var firstResponse = await PostCheckoutAsync(orderingClient, customerId, key);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        var first = await firstResponse.Content.ReadFromJsonAsync<JsonElement>();

        // why: the basket is EMPTY now — the first call cleared it. The replay
        // still answers 201 with the SAME order, because it does no work.
        var secondResponse = await PostCheckoutAsync(orderingClient, customerId, key);
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);
        var second = await secondResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(first.GetProperty("orderId").GetGuid(),
                     second.GetProperty("orderId").GetGuid());
        Assert.Equal(first.GetProperty("total").GetDecimal(),
                     second.GetProperty("total").GetDecimal());

        await using var db = _postgresContainer.CreateOrderingContext();
        Assert.Equal(1, await db.Orders.CountAsync(o => o.CustomerId == customerId));
    }
}
