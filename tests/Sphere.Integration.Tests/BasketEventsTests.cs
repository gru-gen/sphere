using Confluent.Kafka;
using Sphere.Basket.Infrastructure;
using Sphere.Integration.Tests.Basket;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Sphere.Integration.Tests;

[Collection("postgres")]
public sealed class BasketEventsTests : IClassFixture<KafkaFixture>, IDisposable
{
    private readonly string _bootstrap;
    private readonly BasketServiceFactory _factory;

    public BasketEventsTests(PostgresContainer postgres, KafkaFixture kafka)
    {
        _bootstrap = kafka.BootstrapServers;
        _factory = new BasketServiceFactory(postgres, _bootstrap);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Checkout_publishes_one_fact_keyed_by_customer_named_by_the_reply()
    {
        var client = _factory.CreateClient();
        var customerId = Guid.CreateVersion7();
        var productId = Guid.CreateVersion7();
        var add = await client.PostAsJsonAsync($"/api/basket/{customerId}/items",
            new { productId, quantity = 2 });
        Assert.Equal(HttpStatusCode.NoContent, add.StatusCode);

        // why: a test-only peek at the topic — the REAL consumer group lives
        // in the Ordering service and has its own tests.
        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = _bootstrap,
            GroupId = $"test-{Guid.CreateVersion7()}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
        }).Build();
        consumer.Subscribe(KafkaBasketEvents.Topic);

        var key = $"order-{Guid.CreateVersion7()}";
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/checkout")
        {
            Content = JsonContent.Create(new { customerId }),
        };
        request.Headers.Add("Idempotency-Key", key);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = body.GetProperty("orderId").GetGuid();

        // the replay: 202 again, same id — and, below, still ONE fact
        var replay = new HttpRequestMessage(HttpMethod.Post, "/api/checkout")
        {
            Content = JsonContent.Create(new { customerId }),
        };
        replay.Headers.Add("Idempotency-Key", key);
        var again = await client.SendAsync(replay);
        Assert.Equal(HttpStatusCode.Accepted, again.StatusCode);

        var first = consumer.Consume(TimeSpan.FromSeconds(20));
        Assert.NotNull(first);
        Assert.Equal(customerId.ToString(), first!.Message.Key);
        var evt = JsonSerializer.Deserialize<JsonElement>(first.Message.Value);
        Assert.Equal(orderId, evt.GetProperty("checkoutId").GetGuid());
        Assert.Equal(customerId, evt.GetProperty("customerId").GetGuid());
        var item = evt.GetProperty("items")[0];
        Assert.Equal(productId, item.GetProperty("productId").GetGuid());
        Assert.Equal(2, item.GetProperty("quantity").GetInt32());

        // why: the replay announced NOTHING — one checkout, one fact.
        var second = consumer.Consume(TimeSpan.FromSeconds(3));
        Assert.Null(second);
    }
}
