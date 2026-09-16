using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sphere.Ordering.Application.PlaceOrder;
using System.Text.Json;

namespace Sphere.Ordering.Infrastructure;

internal sealed class BasketCheckedOutConsumer(
    KafkaSettings settings,
    IServiceScopeFactory scopes,
    ILogger<BasketCheckedOutConsumer> logger) : BackgroundService
{
    public const string Topic = "sphere.basket.checked-out.v1";
    public const string Group = "ordering";

    private static readonly JsonSerializerOptions Json =
        new(JsonSerializerDefaults.Web);

    private sealed record WireCheckedOut(
        Guid EventId, Guid CheckoutId, Guid CustomerId,
        List<WireLine> Lines, DateTimeOffset OccurredAtUtc);
    private sealed record WireLine(Guid ProductId, int Quantity);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => Task.Run(() => ConsumeLoopAsync(stoppingToken), stoppingToken);

    private async Task ConsumeLoopAsync(CancellationToken cancellationToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = settings.BootstrapServers,
            GroupId = Group,
            // why: WE say when a message is done — after the work, not on a
            // background timer that cannot know whether the work succeeded.
            EnableAutoCommit = false,
            // why: a BRAND-NEW group starts at the oldest fact — the history
            // in the log replays into this consumer on its first day.
            AutoOffsetReset = AutoOffsetReset.Earliest,
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(Topic);
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = consumer.Consume(cancellationToken);
                await HandleAsync(result, cancellationToken);
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

            using var scope = scopes.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                new PlaceOrderCommand(
                    evnt.EventId, evnt.CheckoutId, evnt.CustomerId,
                    evnt.Lines.Select(l => new PlaceOrderLine(l.ProductId, l.Quantity)).ToList()),
                cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "BasketCheckedOut at {Offset} was skipped.", result.TopicPartitionOffset);
        }
    }
}
