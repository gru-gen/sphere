using Sphere.Integration.Tests.Basket;
using Sphere.Integration.Tests.Catalog;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Sphere.Integration.Tests;

// summary: through the front door — the whole shopping loop as ONE test over
// real HTTP semantics: routing, filters, behaviors, ProblemDetails, all of it.
[Collection("postgres")]
public sealed class ShoppingLoopTests : IDisposable
{
    private readonly CatalogServiceFactory _catalogServiceFactory;
    private readonly BasketServiceFactory _basketServiceFactory;
    private readonly OrderingServiceFactory _orderingServiceFactory;

    public ShoppingLoopTests(PostgresContainer container)
    {
        _catalogServiceFactory = new CatalogServiceFactory(container);
        _basketServiceFactory = new BasketServiceFactory(container);
        _orderingServiceFactory = new OrderingServiceFactory(container, _catalogServiceFactory.CreateClient(), _basketServiceFactory.CreateClient());
    }

    public void Dispose()
    {
        _basketServiceFactory.Dispose();
        _catalogServiceFactory.Dispose();
        _orderingServiceFactory.Dispose();
    }

    [Fact]
    public async Task The_full_loop_browse_basket_checkout_read_cancel()
    {
        var catalogClient = _catalogServiceFactory.CreateClient();
        var basketClient = _basketServiceFactory.CreateClient();
        var orderingClient = _orderingServiceFactory.CreateClient();
        var customerId = Guid.CreateVersion7();

        // browse: the CATALOG SERVICE answers now
        var browse = await catalogClient.GetFromJsonAsync<JsonElement>(
            "/api/products?pageSize=1");
        var productId = browse.GetProperty("items")[0].GetProperty("id").GetGuid();

        // basket: the BASKET SERVICE answers now
        var add = await basketClient.PostAsJsonAsync($"/api/basket/{customerId}/items",
            new { productId, quantity = 2 });
        Assert.Equal(HttpStatusCode.NoContent, add.StatusCode);

        // checkout: 201 with a location and a total
        var checkout = await orderingClient.PostAsJsonAsync("/api/checkout", new { customerId });
        Assert.Equal(HttpStatusCode.Created, checkout.StatusCode);
        var placed = await checkout.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = placed.GetProperty("orderId").GetGuid();

        // read: the order exists, Placed, one line, basket now empty
        var order = await orderingClient.GetFromJsonAsync<JsonElement>($"/api/orders/{orderId}");
        Assert.Equal("Placed", order.GetProperty("status").GetString());
        Assert.Equal(1, order.GetProperty("lines").GetArrayLength());
        var basket = await basketClient.GetFromJsonAsync<JsonElement>($"/api/basket/{customerId}");
        Assert.Equal(0, basket.GetProperty("items").GetArrayLength());

        // cancel: 204, then the domain says no with a 422 problem document
        var cancel = await orderingClient.PostAsync($"/api/orders/{orderId}/cancel", null);
        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);
        var again = await orderingClient.PostAsync($"/api/orders/{orderId}/cancel", null);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, again.StatusCode);
        var problem = await again.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("cannot be cancelled",
            problem.GetProperty("detail").GetString());
    }
}
