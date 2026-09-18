using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;
using Sphere.Basket.Contracts;
using System.Text;
using System.Text.Json;

namespace Sphere.Basket.Infrastructure;

public sealed class KafkaBasketEvents : IBasketEvents, IDisposable
{
    public const string Topic = "sphere.basket.checked-out.v1";

    private static readonly JsonSerializerOptions Json =
        new(JsonSerializerDefaults.Web);

    private readonly string _bootstrapServers;
    private readonly string _source;
    private readonly IProducer<string, string> _producer;
    private readonly TimeProvider _clock;
    private readonly ILogger<KafkaBasketEvents> _logger;

    public KafkaBasketEvents(
        string bootstrapServers, string source, TimeProvider clock, ILogger<KafkaBasketEvents> logger)
    {
        _bootstrapServers = bootstrapServers;
        _source = source;
        // why: the taught defaults — every in-sync replica must confirm
        // (acks=all), and retries cannot duplicate (idempotence: the broker
        // discards resends by producer id + sequence number).
        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageTimeoutMs = 3000,
        }).Build();
        _clock = clock;
        _logger = logger;
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
                ],
                new CreateTopicsOptions { RequestTimeout = TimeSpan.FromSeconds(3) });
        }
        catch (CreateTopicsException e) when
            (e.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            // the second start of the same stack — nothing to do.
        }
        catch (KafkaException e)
        {
            _logger.LogWarning(e, "Topic {Topic} was not ensured.", Topic);
        }
    }

    public async Task PublishCheckedOutAsync(BasketSnapshot basket, Guid checkoutId, CancellationToken cancellationToken)
    {
        var evnt = new BasketCheckedOut(
            Guid.CreateVersion7(),
            checkoutId,
            basket.CustomerId,
            basket.Items.Select(i => new BasketCheckedOutLine(i.ProductId, i.Quantity)).ToList(),
            _clock.GetUtcNow());

        var message = new Message<string, string>
        {
            Key = basket.CustomerId.ToString(),
            Value = JsonSerializer.Serialize(evnt, Json),
            //Headers = new Headers
            //{
            //    { "content-type", Encoding.UTF8.GetBytes("application/json") },
            //    { "ce_specversion", Encoding.UTF8.GetBytes("1.0") },
            //    { "ce_id", Encoding.UTF8.GetBytes(evnt.EventId.ToString()) },
            //    { "ce_source", Encoding.UTF8.GetBytes(_source) },
            //    { "ce_type", Encoding.UTF8.GetBytes(Topic) },
            //    { "ce_time", Encoding.UTF8.GetBytes(evnt.CheckedOutAtUtc.ToString("O")) },
            //},
        };

        try
        {
            await _producer.ProduceAsync(Topic, message, cancellationToken);
        }
        catch (KafkaException e)
        {
            _logger.LogWarning(e, "BasketCheckedOut for {CustomerId} was NOT published.", basket.CustomerId);
        }
    }

    public void Dispose() => _producer.Dispose();
}
