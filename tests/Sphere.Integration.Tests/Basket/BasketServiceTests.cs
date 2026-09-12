using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Sphere.Integration.Tests.Basket;

[Collection("postgres")]
public sealed class BasketServiceTests : IDisposable
{
    private readonly BasketServiceFactory _factory;

    public BasketServiceTests(PostgresFixture postgresFixture) =>
        _factory = new BasketServiceFactory(postgresFixture);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Internal_door_reads_the_snapshot_and_clears_idempotently()
    {
        var client = _factory.CreateClient();
        var customerId = Guid.CreateVersion7();
        var productId = Guid.CreateVersion7();

        var add = await client.PostAsJsonAsync($"/api/basket/{customerId}/items",
            new { productId, quantity = 3 });
        Assert.Equal(HttpStatusCode.NoContent, add.StatusCode);

        // the checkout-facing read
        var snapshot = await client.GetFromJsonAsync<JsonElement>(
            $"/internal/baskets/{customerId}");
        var items = snapshot.GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal(3, items[0].GetProperty("quantity").GetInt32());

        // the checkout-facing clear — twice, because DELETE promises idempotence
        var first = await client.DeleteAsync($"/internal/baskets/{customerId}");
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        var second = await client.DeleteAsync($"/internal/baskets/{customerId}");
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);

        var basket = await client.GetFromJsonAsync<JsonElement>(
            $"/api/basket/{customerId}");
        Assert.Equal(0, basket.GetProperty("items").GetArrayLength());
    }
}
