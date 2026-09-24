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

// summary: through the front door — the whole shopping loop as ONE test over
// real HTTP semantics: routing, filters, behaviors, ProblemDetails, all of it.
[Collection("postgres")]
public sealed class ShoppingLoopTests : IDisposable
{
    private readonly CatalogServiceFactory _catalogServiceFactory;
    private readonly HostFactory _hostFactory;

    public ShoppingLoopTests(PostgresContainer container)
    {
        _catalogServiceFactory = new CatalogServiceFactory(container);
        _hostFactory = new HostFactory(container, _catalogServiceFactory.CreateClient());
    }

    public void Dispose()
    {
        _catalogServiceFactory.Dispose();
        _hostFactory.Dispose();
    }

    [Fact]
    public async Task The_full_loop_browse_basket_checkout_read_cancel()
    {
        var catalogClient = _catalogServiceFactory.CreateClient();
        var hostClient = _hostFactory.CreateClient();
        var customerId = Guid.CreateVersion7();

        // browse: the CATALOG SERVICE answers now
        var browse = await catalogClient.GetFromJsonAsync<JsonElement>(
            "/api/products?pageSize=1");
        var productId = browse.GetProperty("items")[0].GetProperty("id").GetGuid();

        // basket: host
        var add = await hostClient.PostAsJsonAsync($"/api/basket/{customerId}/items",
            new { productId, quantity = 2 });
        Assert.Equal(HttpStatusCode.NoContent, add.StatusCode);

        // checkout: 201 with a location and a total
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
        Assert.Contains("cannot be cancelled",
            problem.GetProperty("detail").GetString());
    }

    private sealed class HostFactory(PostgresContainer postgresContainer, HttpClient catalogClient)
        : WebApplicationFactory<HostMarker>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            foreach (var name in new[] { "catalog", "basket", "ordering" })
            {
                builder.UseSetting($"ConnectionStrings:{name}", postgresContainer.ConnectionString);
            }

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
