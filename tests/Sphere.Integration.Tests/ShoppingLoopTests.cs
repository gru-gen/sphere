using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sphere.Integration.Tests.Catalog;
using Sphere.Ordering.Application.Checkout;
using Sphere.Ordering.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Sphere.Integration.Tests;

// summary: the journey after the first cut — the SAME loop, now across two
// processes. Browse talks to the Catalog service; basket, checkout, and
// orders talk to the monolith; checkout crosses the wire for prices.
[Collection("postgres")]
public sealed class ShoppingLoopTests : IDisposable
{
    private readonly CatalogServiceFactory _catalogServiceFactory;
    private readonly HostFactory _hostFactory;

    public ShoppingLoopTests(PostgresFixture postgresFixture)
    {
        _catalogServiceFactory = new CatalogServiceFactory(postgresFixture);
        _hostFactory = new HostFactory(postgresFixture, _catalogServiceFactory.CreateClient());
    }

    [Fact]
    public async Task The_full_loop_browse_basket_checkout_read_cancel()
    {
        var catalogClient = _catalogServiceFactory.CreateClient();
        var hostClient = _hostFactory.CreateClient();
        var customerId = Guid.CreateVersion7();

        // browse: the CATALOG SERVICE answers now
        var browse = await catalogClient.GetFromJsonAsync<JsonElement>("/api/products?pageSize=1");
        var productId = browse.GetProperty("items")[0].GetProperty("id").GetGuid();

        // basket: add two of it
        var add = await hostClient.PostAsJsonAsync($"/api/basket/{customerId}/items",
            new { productId, quantity = 2 });
        Assert.Equal(HttpStatusCode.NoContent, add.StatusCode);

        // checkout: the monolith calls the catalog service for prices
        var checkout = await hostClient.PostAsJsonAsync("/api/checkout", new { customerId });
        Assert.Equal(HttpStatusCode.Created, checkout.StatusCode);
        var placed = await checkout.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = placed.GetProperty("orderId").GetGuid();

        // read: the order exists, Placed, one line, basket now empty
        var order = await hostClient.GetFromJsonAsync<JsonElement>($"/api/orders/{orderId}");
        Assert.Equal("Placed", order.GetProperty("status").GetString());
        Assert.Equal(1, order.GetProperty("lines").GetArrayLength());

        var basket = await hostClient.GetFromJsonAsync<JsonElement>($"/api/basket/{customerId}");
        Assert.Equal(0, basket.GetProperty("items").GetArrayLength());

        // cancel: 204, then the domain says no with a 422 problem document
        var cancel = await hostClient.PostAsync($"/api/orders/{orderId}/cancel", null);
        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);

        var again = await hostClient.PostAsync($"/api/orders/{orderId}/cancel", null);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, again.StatusCode);
        var problem = await again.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("cannot be cancelled", problem.GetProperty("detail").GetString());
    }

    public void Dispose()
    {
        _hostFactory.Dispose();
        _catalogServiceFactory.Dispose();
    }

    // summary: the monolith host minus Catalog — and with its price client
    // swapped for one that talks to the test-hosted catalog service.
    private sealed class HostFactory(PostgresFixture postgresFixture, HttpClient catalogClient)
        : WebApplicationFactory<HostMarker>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            foreach (var name in new[] { "basket", "ordering" })
            {
                builder.UseSetting($"ConnectionStrings:{name}", postgresFixture.ConnectionString);
            }

            // why: the module refuses to start without it; the value is unused
            // because the client below already carries the test address.
            builder.UseSetting("Catalog:BaseUrl", "http://catalog.test");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IProductPriceReader>();
                services.AddSingleton<IProductPriceReader>(
                    new CatalogHttpPriceReader(catalogClient));
            });
        }
    }
}
