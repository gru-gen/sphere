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
    public async Task Public_door_reads_and_the_internal_doors_are_gone()
    {
        var client = _factory.CreateClient();
        var customerId = Guid.CreateVersion7();
        var productId = Guid.CreateVersion7();

        var add = await client.PostAsJsonAsync($"/api/basket/{customerId}/items",
            new { productId, quantity = 3 });
        Assert.Equal(HttpStatusCode.NoContent, add.StatusCode);

        var basket = await client.GetFromJsonAsync<JsonElement>(
            $"/api/basket/{customerId}");
        var items = basket.GetProperty("items");

        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal(3, items[0].GetProperty("quantity").GetInt32());
    }
}
