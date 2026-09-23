using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Sphere.Integration.Tests;

// summary: through the front door — the whole shopping loop as ONE test over
// real HTTP semantics: routing, filters, behaviors, ProblemDetails, all of it.
[Collection("postgres")]
public sealed class ShoppingLoopTests : IDisposable
{
    private readonly HostFactory _hostFactory;

    public ShoppingLoopTests(PostgresContainer container) => _hostFactory = new HostFactory(container);

    public void Dispose() => _hostFactory.Dispose();

    [Fact]
    public async Task The_full_loop_browse_basket_checkout_read_cancel()
    {
        var client = _hostFactory.CreateClient();
        var customerId = Guid.CreateVersion7();

        // browse: the seeded catalog answers
        var browse = await client.GetFromJsonAsync<JsonElement>(
            "/api/products?pageSize=1");
        var productId = browse.GetProperty("items")[0].GetProperty("id").GetGuid();

        var add = await client.PostAsJsonAsync($"/api/basket/{customerId}/items",
            new { productId, quantity = 2 });
        Assert.Equal(HttpStatusCode.NoContent, add.StatusCode);

        // checkout: 201 with a location and a total
        var checkout = await client.PostAsJsonAsync("/api/checkout", new { customerId });
        Assert.Equal(HttpStatusCode.Created, checkout.StatusCode);
        var placed = await checkout.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = placed.GetProperty("orderId").GetGuid();

        // read: the order exists, Placed, one line, basket now empty
        var order = await client.GetFromJsonAsync<JsonElement>($"/api/orders/{orderId}");
        Assert.Equal("Placed", order.GetProperty("status").GetString());
        Assert.Equal(1, order.GetProperty("lines").GetArrayLength());
        var basket = await client.GetFromJsonAsync<JsonElement>($"/api/basket/{customerId}");
        Assert.Equal(0, basket.GetProperty("items").GetArrayLength());

        // cancel: 204, then the domain says no with a 422 problem document
        var cancel = await client.PostAsync($"/api/orders/{orderId}/cancel", null);
        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);
        var again = await client.PostAsync($"/api/orders/{orderId}/cancel", null);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, again.StatusCode);
        var problem = await again.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("cannot be cancelled",
            problem.GetProperty("detail").GetString());
    }

    private sealed class HostFactory(PostgresContainer postgresContainer)
        : WebApplicationFactory<HostMarker>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            foreach (var name in new[] { "catalog", "basket", "ordering" })
            {
                builder.UseSetting($"ConnectionStrings:{name}", postgresContainer.ConnectionString);
            }
        }
    }
}
