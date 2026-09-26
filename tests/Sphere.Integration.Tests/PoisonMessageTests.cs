using Confluent.Kafka;
using Sphere.Integration.Tests.Catalog;
using Sphere.Ordering.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Sphere.Integration.Tests;

[Collection("postgres")]
public sealed class PoisonMessageTests : IClassFixture<KafkaFixture>, IDisposable
{
    private readonly string _bootstrap;
    private readonly CatalogServiceFactory _catalog;
    private readonly OrderingServiceFactory _ordering;

    public PoisonMessageTests(PostgresContainer fixture, KafkaFixture kafka)
    {
        _bootstrap = kafka.BootstrapServers;
        _catalog = new CatalogServiceFactory(fixture);
        _ordering = new OrderingServiceFactory(
            fixture, _catalog.CreateClient(), _bootstrap);
    }

    public void Dispose()
    {
        _ordering.Dispose();
        _catalog.Dispose();
    }

    [Fact]
    public async Task Poison_lands_in_the_letterbox_and_the_good_fact_still_becomes_an_order()
    {
        // why: the consumer subscribes on host start; starting it FIRST also
        // creates the retry and dead-letter topics through the lifecycle door.
        var orderingClient = _ordering.CreateClient();

        using var producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = _bootstrap,
            Acks = Acks.All,
        }).Build();

        // the topic exists because the basket ensures it in production; here
        // the test plays the producer and creates it explicitly.
        using (var admin = new AdminClientBuilder(
            new AdminClientConfig { BootstrapServers = _bootstrap }).Build())
        {
            try
            {
                await admin.CreateTopicsAsync([new Confluent.Kafka.Admin.TopicSpecification
                {
                    Name = BasketCheckedOutConsumer.Topic,
                    NumPartitions = 3,
                    ReplicationFactor = 1,
                }]);
            }
            catch (Confluent.Kafka.Admin.CreateTopicsException)
            {
                // already there — fine.
            }
        }

        // poison first: not JSON, never will be
        await producer.ProduceAsync(BasketCheckedOutConsumer.Topic,
            new Message<string, string> { Key = "poison", Value = "definitely not json" });

        // then a real fact, hand-rolled the way the basket writes it
        var browse = await _catalog.CreateClient().GetFromJsonAsync<JsonElement>(
            "/api/products?pageSize=1");
        var productId = browse.GetProperty("items")[0].GetProperty("id").GetGuid();
        var customerId = Guid.CreateVersion7();
        var orderId = Guid.CreateVersion7();
        var fact = JsonSerializer.Serialize(new
        {
            eventId = Guid.CreateVersion7(),
            checkoutId = orderId,
            customerId,
            lines = new[] { new { productId, quantity = 1 } },
            occurredAtUtc = DateTimeOffset.UtcNow,
        });
        await producer.ProduceAsync(BasketCheckedOutConsumer.Topic,
            new Message<string, string> { Key = customerId.ToString(), Value = fact });

        // the good fact still becomes an order — the lane stayed open
        var deadline = DateTime.UtcNow.AddSeconds(30);
        HttpResponseMessage? read = null;
        while (DateTime.UtcNow < deadline)
        {
            read = await orderingClient.GetAsync($"/api/orders/{orderId}");
            if (read.StatusCode == HttpStatusCode.OK)
            {
                break;
            }
            await Task.Delay(250);
        }
        Assert.Equal(HttpStatusCode.OK, read!.StatusCode);

        // and the poison sits in the letterbox with its history in the headers
        using var peek = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = _bootstrap,
            GroupId = $"test-{Guid.CreateVersion7()}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
        }).Build();
        peek.Subscribe(BasketCheckedOutConsumer.DeadLetterTopic);
        var dead = peek.Consume(TimeSpan.FromSeconds(30));
        Assert.NotNull(dead);
        Assert.Equal("definitely not json", dead!.Message.Value);
        Assert.Equal("3", Encoding.UTF8.GetString(
            dead.Message.Headers.GetLastBytes("x-attempts")));
        Assert.Equal(nameof(JsonException), Encoding.UTF8.GetString(
            dead.Message.Headers.GetLastBytes("x-last-error")));
    }
}
