using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Sphere.Integration.Tests.Basket;

[Collection("postgres")]
public sealed class BasketCheckoutTests(PostgresFixture postgresFixture)
{
    private static Task<HttpResponseMessage> PostCheckoutAsync(
        HttpClient httpClient, Guid customerId, string? key = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/checkout")
        {
            Content = JsonContent.Create(new { customerId })
        };

        if (key is not null)
        {
            request.Headers.Add("Idempotency-Key", key);
        }

        return httpClient.SendAsync(request);
    }

    [Fact]
    public async Task Checkout_answers_202_clears_the_basket_and_announces_once()
    {
        using var factory = new BasketServiceFactory(postgresFixture);
        var client = factory.CreateClient();
        var customerId = Guid.CreateVersion7();
        var productId = Guid.CreateVersion7();
        var add = await client.PostAsJsonAsync($"/api/basket/{customerId}/items",
            new { productId, quantity = 2 });
        Assert.Equal(HttpStatusCode.NoContent, add.StatusCode);

        var response = await PostCheckoutAsync(client, customerId);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = body.GetProperty("orderId").GetGuid();
        Assert.Equal($"/api/orders/{orderId}", response.Headers.Location!.ToString());

        var (snapshot, publishedId) = Assert.Single(factory.Events.Published);
        Assert.Equal(orderId, publishedId);
        var item = Assert.Single(snapshot.Items);
        Assert.Equal(productId, item.ProductId);
        Assert.Equal(2, item.Quantity);

        var basket = await client.GetFromJsonAsync<JsonElement>(
            $"/api/basket/{customerId}");
        Assert.Equal(0, basket.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task The_same_key_replays_the_same_order_id_and_announces_nothing_new()
    {
        using var factory = new BasketServiceFactory(postgresFixture);
        var client = factory.CreateClient();
        var customerId = Guid.CreateVersion7();
        var productId = Guid.CreateVersion7();
        var add = await client.PostAsJsonAsync($"/api/basket/{customerId}/items",
            new { productId, quantity = 2 });
        Assert.Equal(HttpStatusCode.NoContent, add.StatusCode);
        var key = $"order-{Guid.CreateVersion7()}";

        var first = await PostCheckoutAsync(client, customerId, key);
        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);

        var second = await PostCheckoutAsync(client, customerId, key);
        Assert.Equal(HttpStatusCode.Accepted, second.StatusCode);

        Assert.Equal(first.Headers.Location, second.Headers.Location);
        Assert.Single(factory.Events.Published);
    }

    [Fact]
    public async Task An_empty_basket_is_refused_with_422_and_does_not_burn_the_key()
    {
        using var factory = new BasketServiceFactory(postgresFixture);
        var client = factory.CreateClient();
        var customerId = Guid.CreateVersion7();
        var key = $"order-{Guid.CreateVersion7()}";

        var response = await PostCheckoutAsync(client, customerId, key);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Empty(factory.Events.Published);

        var add = await client.PostAsJsonAsync($"/api/basket/{customerId}/items",
            new { productId = Guid.CreateVersion7(), quantity = 1 });
        Assert.Equal(HttpStatusCode.NoContent, add.StatusCode);
        var retry = await PostCheckoutAsync(client, customerId, key);
        Assert.Equal(HttpStatusCode.Accepted, retry.StatusCode);
        Assert.Single(factory.Events.Published);
    }
}
