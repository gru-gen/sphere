using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sphere.Ordering.Application.PlaceOrder;
using System.Text;
using System.Text.Json;

namespace Sphere.Ordering.Infrastructure;

internal class BasketCheckedOutConsumer(
    KafkaSettings kafkaSettings,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<BasketCheckedOutConsumer> logger) : BackgroundService
{
    public const string Topic = "sphere.basket.checked-out.v1";
    public const string Group = "ordering";

    public const string RetryTopic = Topic + ".ordering.retry";
    public const string DeadLetterTopic = Topic + ".ordering.dlq";
    public const int MaxAttempts = 3;

    private static readonly JsonSerializerOptions Json =
        new(JsonSerializerDefaults.Web);

    private sealed record WireCheckedOut(
        Guid EventId, Guid CheckoutId, Guid CustomerId,
        List<WireItem> Items, DateTimeOffset OccurredAtUtc);

    private sealed record WireItem(Guid ProductId, int Quantity);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => Task.Run(() => ConsumeLoopAsync(stoppingToken), stoppingToken);

    private async Task ConsumeLoopAsync(CancellationToken cancellationToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaSettings.BootstrapServers,
            GroupId = Group,
            EnableAutoCommit = true,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        using var producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = kafkaSettings.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageTimeoutMs = 3000,
        }).Build();

        consumer.Subscribe([Topic, RetryTopic]);
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = consumer.Consume(cancellationToken);
                if (result.Topic == RetryTopic)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                }

                try
                {
                    await HandleAsync(result, cancellationToken);
                }
                catch (Exception e)
                {
                    await ParkAsync(producer, result, e, cancellationToken);
                }

                consumer.Commit(result);
            }
        }
        catch (OperationCanceledException)
        {
            // the host is stopping — not an error.
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task HandleAsync(ConsumeResult<string, string> result, CancellationToken cancellationToken)
    {
        try
        {
            var evnt = JsonSerializer.Deserialize<WireCheckedOut>(result.Message.Value, Json)
                ?? throw new JsonException("Empty payload.");

            using var scope = serviceScopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                 new PlaceOrderCommand(
                    evnt.EventId, evnt.CheckoutId, evnt.CustomerId,
                    evnt.Items.Select(l => new PlaceOrderItem(l.ProductId, l.Quantity)).ToList()),
                 cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "BasketCheckedOut at {Offset} was skipped.", result.TopicPartitionOffset);
        }
    }

    private async Task ParkAsync(IProducer<string, string> producer,
        ConsumeResult<string, string> result, Exception error, CancellationToken cancellationToken)
    {
        var attempts = ReadAttempts(result.Message.Headers) + 1;
        var target = attempts < MaxAttempts ? RetryTopic : DeadLetterTopic;

        var message = new Message<string, string>
        {
            Key = result.Message.Key,
            Value = result.Message.Value,
            Headers = new Headers
            {
                { "x-attempts", Encoding.UTF8.GetBytes(attempts.ToString()) },
                { "x-last-error", Encoding.UTF8.GetBytes(error.GetType().Name) }
            }
        };

        await producer.ProduceAsync(target, message, cancellationToken);

        logger.LogWarning(error, "Message at {Offset} parked on {Target} (attempt {Attempts}).",
            result.TopicPartitionOffset, target, attempts);
    }

    private static int ReadAttempts(Headers headers)
        => headers.TryGetLastBytes("x-attempt", out var bytes)
        ? int.Parse(Encoding.UTF8.GetString(bytes))
        : 0;
}
