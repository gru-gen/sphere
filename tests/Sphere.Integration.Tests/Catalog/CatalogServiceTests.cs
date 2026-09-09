using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Sphere.Integration.Tests.Catalog;

[Collection("postgres")]
public sealed class CatalogServiceTests(PostgresFixture postgresFixture)
{
    [Fact]
    public async Task Browse_health_and_internal_prices_answer_from_catalog_db()
    {
        using var factory = new CatalogServiceFactory(postgresFixture);
        var client = factory.CreateClient();

        var ready = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);

        var browse = await client.GetFromJsonAsync<JsonElement>(
            "/api/products?pageSize=1");
        var productId = browse.GetProperty("items")[0].GetProperty("id").GetGuid();

        var response = await client.PostAsJsonAsync("/internal/prices",
            new { productIds = new[] { productId, Guid.CreateVersion7() } });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var lines = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, lines.GetArrayLength());
        Assert.Equal(productId, lines[0].GetProperty("productId").GetGuid());
        Assert.True(lines[0].GetProperty("price").GetDecimal() > 0);
    }
}
