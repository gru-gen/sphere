using MassTransit;
using Sphere.Notification.Service.Messages;

namespace Sphere.Notification.Service.Consumers;

internal sealed class PushConsumer(
    NotificationDbContext notificationDbContext,
    TimeProvider clock,
    ILogger<PushConsumer> logger) : IConsumer<OrderConfirmationReady>
{
    public async Task Consume(ConsumeContext<OrderConfirmationReady> context)
    {
        var message = context.Message;
        var text = $"Order placed - tap to track {message.OrderId}.";
        notificationDbContext.Notifications.Add(new SentNotification
        {
            Id = Guid.CreateVersion7(),
            OrderId = message.OrderId,
            CustomerId = message.CustomerId,
            Channel = "push",
            Subject = null,
            Body = text,
            CreatedAtUtc = clock.GetUtcNow(),
        });

        try
        {
            await notificationDbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (DbUpdateException)
        {
            var alreadySent = await notificationDbContext.Notifications.AsNoTracking().AnyAsync(
                n => n.OrderId == message.OrderId && n.Channel == "push",
                CancellationToken.None);
            if (!alreadySent)
            {
                throw;
            }
            return;
        }

        logger.LogInformation("PUSH to customer {CustomerId}: \"{Text}\" (simulated).",
            message.CustomerId, text);
    }
}
