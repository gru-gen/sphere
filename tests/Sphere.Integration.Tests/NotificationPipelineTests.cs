using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sphere.Notification.Service.Consumers;
using Sphere.Notification.Service.Data;
using Sphere.Notification.Service.Messages;

namespace Sphere.Integration.Tests;

[Collection("postgres")]
public sealed class NotificationPipelineTests(PostgresFixture fixture)
{
    private ServiceProvider BuildProvider() => new ServiceCollection()
        .AddLogging()
        .AddSingleton(TimeProvider.System)
        .AddDbContext<NotificationDbContext>(o =>
            o.UseNpgsql(fixture.NotificationConnectionString))
        .AddMassTransitTestHarness(x =>
        {
            x.AddConsumer<SendOrderConfirmationConsumer>();
            x.AddConsumer<EmailConsumer>();
            x.AddConsumer<SmsConsumer>();
            x.AddConsumer<PushConsumer>();
        })
        .BuildServiceProvider(true);

    private async Task<List<SentNotification>> WaitForRowsAsync(Guid orderId, int count)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            await using var db = fixture.CreateNotificationContext();
            var rows = await db.Notifications.AsNoTracking()
                .Where(n => n.OrderId == orderId).ToListAsync();
            if (rows.Count >= count)
            {
                return rows;
            }
            await Task.Delay(100);
        }
        Assert.Fail($"Expected {count} sent_notifications rows for {orderId}.");
        return [];
    }

    [Fact]
    public async Task One_command_becomes_three_channel_rows()
    {
        await using var provider = BuildProvider();
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var orderId = Guid.CreateVersion7();
        var command = new Notification.Service.Messages.SendOrderConfirmation(
            orderId, Guid.CreateVersion7(), 59.90m, "EUR");
        await harness.Bus.Publish(command);

        Assert.True(await harness.Consumed.Any<Notification.Service.Messages.SendOrderConfirmation>());
        // the fan-out event left stage one...
        Assert.True(await harness.Published.Any<OrderConfirmationReady>());

        // ...and every channel wrote its own row
        var rows = await WaitForRowsAsync(orderId, 3);
        Assert.Equal(["email", "push", "sms"],
            rows.Select(r => r.Channel).OrderBy(c => c).ToArray());
        var email = rows.Single(r => r.Channel == "email");
        Assert.Contains("59.90 EUR", email.Body);
    }

    [Fact]
    public async Task A_redelivered_confirmation_writes_nothing_twice()
    {
        await using var provider = BuildProvider();
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var orderId = Guid.CreateVersion7();
        var ready = new OrderConfirmationReady(
            orderId, Guid.CreateVersion7(), "subject", "body");

        await harness.Bus.Publish(ready);
        await WaitForRowsAsync(orderId, 3);

        // why: the redelivery - same event, byte for byte. The unique index
        // answers, the consumers stay silent, and the count stays three.
        await harness.Bus.Publish(ready);
        Assert.True(await harness.Consumed.SelectAsync<OrderConfirmationReady>().Count() >= 6);

        await using var db = fixture.CreateNotificationContext();
        Assert.Equal(3, await db.Notifications.CountAsync(n => n.OrderId == orderId));
    }
}
