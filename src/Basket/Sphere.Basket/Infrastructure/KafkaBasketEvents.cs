using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;
using Sphere.Basket.Contracts;
using System.Text.Json;

namespace Sphere.Basket.Infrastructure;

public sealed class KafkaBasketEvents : IBasketEvents, IDisposable
{
    public const string Topic = "sphere.basket.checked-out.v1";

    private static readonly JsonSerializerOptions Json =
        new(JsonSerializerOptions.Web);

    private readonly string _bootstrapServers;
    private readonly IProducer<string, string> _producer;
    private readonly TimeProvider _clock;
    private readonly ILogger<KafkaBasketEvents> _logger;

    public KafkaBasketEvents(
        string bootstrapServers, TimeProvider clock, ILogger<KafkaBasketEvents> logger)
    {
        _bootstrapServers = bootstrapServers;
        _clock = clock;
        _logger = logger;
        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageTimeoutMs = 3000,
        }).Build();
    }

    public async Task EnsureTopicAsync()
    {
        using var admin = new AdminClientBuilder(
            new AdminClientConfig { BootstrapServers = _bootstrapServers }).Build();
        try
        {
            await admin.CreateTopicsAsync(
                [
                    new TopicSpecification
                    {
                        Name = Topic,
                        NumPartitions = 3,
                        ReplicationFactor = 1,
                    }
                ], new CreateTopicsOptions { RequestTimeout = TimeSpan.FromSeconds(3) });
        }
        catch (CreateTopicsException e) when (
            e.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            // the second start of the same stack — nothing to do.
        }
        catch (KafkaException e)
        {
            _logger.LogWarning(e, "Topic {Topic} was not ensured — is Kafka running.", Topic);
        }
    }

    public async Task PublishCheckedOutAsync(BasketSnapshot basketSnapshot, Guid checkoutId, CancellationToken cancellationToken)
    {
        var evnt = new BasketCheckedOut(
            Guid.CreateVersion7(),
            checkoutId,
            basketSnapshot.CustomerId,
            basketSnapshot.Items.Select(i => new BasketCheckedOutItem(i.ProductId, i.Quantity)).ToList(),
            _clock.GetUtcNow());

        var message = new Message<string, string>
        {
            Key = basketSnapshot.CustomerId.ToString(),
            Value = JsonSerializer.Serialize(evnt, Json)
        };

        try
        {
            await _producer.ProduceAsync(Topic, message, cancellationToken);
        }
        catch (KafkaException e)
        {
            _logger.LogWarning(e,
               "BasketCheckedOut for {CustomerId} was NOT published.", basketSnapshot.CustomerId);
        }
    }

    public void Dispose() => _producer.Dispose();
}
