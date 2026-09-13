using Confluent.Kafka;
using Sphere.Basket.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Sphere.Integration.Tests.Basket;

[Collection("postgres")]
public sealed class BasketEventsTests : IClassFixture<KafkaFixture>, IDisposable
{
    private readonly string _bootstrap;
    private readonly BasketServiceFactory _basketServiceFactory;

    public BasketEventsTests(PostgresFixture postgres, KafkaFixture kafka)
    {
        _bootstrap = kafka.BootstrapServers;
        _basketServiceFactory = new BasketServiceFactory(postgres, _bootstrap);
    }

    public void Dispose() => _basketServiceFactory.Dispose();

    [Fact]
    public async Task Clearing_a_basket_publishes_one_fact_keyed_by_customer()
    {
        var client = _basketServiceFactory.CreateClient();
        var customerId = Guid.CreateVersion7();
        var productId = Guid.CreateVersion7();
        var add = await client.PostAsJsonAsync($"/api/basket/{customerId}/items",
            new { productId, quantity = 2 });
        Assert.Equal(HttpStatusCode.NoContent, add.StatusCode);

        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = _bootstrap,
            GroupId = $"test-{Guid.CreateVersion7()}",
            AutoOffsetReset = AutoOffsetReset.Earliest
        }).Build();

        consumer.Subscribe(KafkaBasketEvents.Topic);

        var clear = await client.DeleteAsync($"/internal/baskets/{customerId}");
        Assert.Equal(HttpStatusCode.NoContent, clear.StatusCode);

        // the idempotent re-clear: still 204 — and, below, still ONE event.
        var again = await client.DeleteAsync($"/internal/baskets/{customerId}");
        Assert.Equal(HttpStatusCode.NoContent, again.StatusCode);

        var first = consumer.Consume(TimeSpan.FromSeconds(20));
        Assert.NotNull(first);
        Assert.Equal(customerId.ToString(), first.Message.Key);
        var evnt = JsonSerializer.Deserialize<JsonElement>(first.Message.Value);
        Assert.Equal(customerId, evnt.GetProperty("customerId").GetGuid());
        var line = evnt.GetProperty("lines")[0];
        Assert.Equal(productId, line.GetProperty("productId").GetGuid());
        Assert.Equal(2, line.GetProperty("quantity").GetInt32());

        // why: an empty clear announced NOTHING — one checkout, one fact.
        var second = consumer.Consume(TimeSpan.FromSeconds(3));
        Assert.Null(second);
    }
}
