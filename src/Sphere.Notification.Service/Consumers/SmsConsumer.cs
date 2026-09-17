using MassTransit;
using Sphere.Notification.Service.Messages;

namespace Sphere.Notification.Service.Consumers;

internal sealed class SmsConsumer(
    NotificationDbContext notificationDbContext,
    TimeProvider clock,
    ILogger<SmsConsumer> logger) : IConsumer<OrderConfirmationReady>
{
    public async Task Consume(ConsumeContext<OrderConfirmationReady> context)
    {
        var message = context.Message;
        var text = $"ShopSphere: order {message.OrderId} is placed.";
        notificationDbContext.Notifications.Add(new SentNotification
        {
            Id = Guid.CreateVersion7(),
            OrderId = message.OrderId,
            CustomerId = message.CustomerId,
            Channel = "sms",
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
                n => n.OrderId == message.OrderId && n.Channel == "sms",
                CancellationToken.None);
            if (!alreadySent)
            {
                throw;
            }
            return;
        }

        logger.LogInformation("SMS to customer {CustomerId}: \"{Text}\" (simulated).",
            message.CustomerId, text);
    }
}
