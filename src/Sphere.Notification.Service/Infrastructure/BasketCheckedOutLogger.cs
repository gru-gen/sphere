using Confluent.Kafka;
using System.Text.Json;

namespace Sphere.Notification.Service.Infrastructure;

internal sealed class BasketCheckedOutLogger(
    KafkaSettings settings,
    ILogger<BasketCheckedOutLogger> logger) : BackgroundService
{
    public const string Topic = "sphere.basket.checked-out.v1";
    public const string Group = "notification";

    private static readonly JsonSerializerOptions Json =
        new(JsonSerializerDefaults.Web);

    private sealed record WireCheckedOut(Guid CustomerId, List<WireLine> Lines);
    private sealed record WireLine(Guid ProductId, int Quantity);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);

    private void ConsumeLoop(CancellationToken cancellationToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = settings.BootstrapServers,
            GroupId = Group,
            EnableAutoCommit = true,
            AutoOffsetReset = AutoOffsetReset.Earliest,
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(Topic);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = consumer.Consume(cancellationToken);
                try
                {
                    var evnt = JsonSerializer.Deserialize<WireCheckedOut>(
                        result.Message.Value, Json);
                    if (evnt is not null)
                    {
                        logger.LogInformation("Heard on the log: customer {CustomerId} checked out {Count} line(s).",
                            evnt.CustomerId, evnt.Lines.Count);
                    }
                }
                catch (JsonException)
                {
                    logger.LogWarning("Unreadable fact at {Offset} skipped.",
                        result.TopicPartitionOffset);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // the host is stopping - not an error.
        }
        finally
        {
            consumer.Close();
        }
    }
}
