using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;
using Sphere.Ordering.Application.Events;
using Sphere.Ordering.Contracts.Events;
using System.Text.Json;

namespace Sphere.Ordering.Infrastructure;

internal sealed class KafkaOrderingEvents : IOrderingEvents, IDisposable
{
    public const string Topic = "sphere.ordering.order-placed.v1";

    private static readonly JsonSerializerOptions Json =
        new(JsonSerializerDefaults.Web);

    private readonly string _bootstrapServers;
    private readonly string _source;
    private readonly IProducer<string, string> _producer;
    private readonly TimeProvider _clock;
    private readonly ILogger<KafkaOrderingEvents> _log;

    public KafkaOrderingEvents(
        string bootstrapServers, string source, TimeProvider clock, ILogger<KafkaOrderingEvents> log)
    {
        _bootstrapServers = bootstrapServers;
        _source = source;
        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageTimeoutMs = 3000,
        }).Build();
        _clock = clock;
        _log = log;
    }

    public async Task EnsureTopicAsync()
    {
        using var admin = new AdminClientBuilder(
            new AdminClientConfig { BootstrapServers = _bootstrapServers }).Build();
        try
        {
            await admin.CreateTopicsAsync(
                [new TopicSpecification
                {
                    Name = Topic,
                    NumPartitions = 3,
                    ReplicationFactor = 1,
                }],
                new CreateTopicsOptions { RequestTimeout = TimeSpan.FromSeconds(3) });
        }
        catch (CreateTopicsException e) when (
            e.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            // the second start of the same stack — nothing to do.
        }
        catch (KafkaException e)
        {
            _log.LogWarning(e, "Topic {Topic} was not ensured — is Kafka running?", Topic);
        }
    }

    public async Task PublishOrderPlacedAsync(
        Guid orderId, Guid customerId, decimal total, string currency, CancellationToken ct)
    {
        var evt = new OrderPlaced(
            Guid.CreateVersion7(), orderId, customerId, total, currency, _clock.GetUtcNow());

        var message = new Message<string, string>
        {
            Key = customerId.ToString(),
            Value = JsonSerializer.Serialize(evt, Json),
            //Headers = new Headers
            //{
            //    { "content-type", Encoding.UTF8.GetBytes("application/json") },
            //    { "ce_specversion", Encoding.UTF8.GetBytes("1.0") },
            //    { "ce_id", Encoding.UTF8.GetBytes(evt.EventId.ToString()) },
            //    { "ce_source", Encoding.UTF8.GetBytes(_source) },
            //    { "ce_type", Encoding.UTF8.GetBytes(Topic) },
            //    { "ce_time", Encoding.UTF8.GetBytes(evt.PlacedAtUtc.ToString("O")) },
            //},
        };

        try
        {
            await _producer.ProduceAsync(Topic, message, ct);
        }
        catch (KafkaException e)
        {
            _log.LogWarning(e, "OrderPlaced for {OrderId} was NOT published.", orderId);
        }
    }

    public void Dispose() => _producer.Dispose();
}
