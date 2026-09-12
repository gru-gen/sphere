using Sphere.Integration.Tests.Basket;
using Sphere.Integration.Tests.Catalog;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Sphere.Integration.Tests;

// summary: the journey after the SECOND cut — the same loop, now across three
// processes. Browse talks to Catalog, the basket steps talk to Basket, and
// checkout in the monolith crosses the wire twice: prices in, clear out.
[Collection("postgres")]
public sealed class ShoppingLoopTests : IDisposable
{
    private readonly CatalogServiceFactory _catalogServiceFactory;
    private readonly BasketServiceFactory _basketServiceFactory;
    private readonly MonolithFactory _monolithFactory;

    public ShoppingLoopTests(PostgresFixture postgresFixture)
    {
        _catalogServiceFactory = new CatalogServiceFactory(postgresFixture);
        _basketServiceFactory = new BasketServiceFactory(postgresFixture);
        _monolithFactory = new MonolithFactory(postgresFixture, _catalogServiceFactory.CreateClient(), _basketServiceFactory.CreateClient());
    }

    [Fact]
    public async Task The_full_loop_browse_basket_checkout_read_cancel()
    {
        var catalogClient = _catalogServiceFactory.CreateClient();
        var basketClient = _basketServiceFactory.CreateClient();
        var monolithCLient = _monolithFactory.CreateClient();
        var customerId = Guid.CreateVersion7();

        // browse: the CATALOG SERVICE answers
        var browse = await catalogClient.GetFromJsonAsync<JsonElement>("/api/products?pageSize=1");
        var productId = browse.GetProperty("items")[0].GetProperty("id").GetGuid();

        // basket: the BASKET SERVICE answers now
        var add = await basketClient.PostAsJsonAsync($"/api/basket/{customerId}/items",
            new { productId, quantity = 2 });
        Assert.Equal(HttpStatusCode.NoContent, add.StatusCode);

        // checkout: the monolith calls Catalog for prices, then Basket to clear
        var checkout = await monolithCLient.PostAsJsonAsync("/api/checkout", new { customerId });
        Assert.Equal(HttpStatusCode.Created, checkout.StatusCode);
        var placed = await checkout.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = placed.GetProperty("orderId").GetGuid();

        // read: the order exists, Placed, one line — and the REMOTE basket is empty
        var order = await monolithCLient.GetFromJsonAsync<JsonElement>($"/api/orders/{orderId}");
        Assert.Equal("Placed", order.GetProperty("status").GetString());
        Assert.Equal(1, order.GetProperty("lines").GetArrayLength());

        var basket = await basketClient.GetFromJsonAsync<JsonElement>($"/api/basket/{customerId}");
        Assert.Equal(0, basket.GetProperty("items").GetArrayLength());

        // cancel: 204, then the domain says no with a 422 problem document
        var cancel = await monolithCLient.PostAsync($"/api/orders/{orderId}/cancel", null);
        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);

        var again = await monolithCLient.PostAsync($"/api/orders/{orderId}/cancel", null);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, again.StatusCode);
        var problem = await again.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("cannot be cancelled", problem.GetProperty("detail").GetString());
    }

    public void Dispose()
    {
        _monolithFactory.Dispose();
        _catalogServiceFactory.Dispose();
        _basketServiceFactory.Dispose();
    }
}
