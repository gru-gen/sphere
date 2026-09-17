using MassTransit;
using Sphere.Notification.Service.Messages;

namespace Sphere.Notification.Service.Consumers;

internal sealed class EmailConsumer(
    NotificationDbContext notificationDbContext,
    TimeProvider clock,
    ILogger<EmailConsumer> logger) : IConsumer<OrderConfirmationReady>
{
    public async Task Consume(ConsumeContext<OrderConfirmationReady> context)
    {
        var message = context.Message;
        notificationDbContext.Notifications.Add(new SentNotification
        {
            Id = Guid.CreateVersion7(),
            OrderId = message.OrderId,
            CustomerId = message.CustomerId,
            Channel = "email",
            Subject = message.Subject,
            Body = message.Body,
            CreatedAtUtc = clock.GetUtcNow()
        });

        try
        {
            await notificationDbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (DbUpdateException)
        {
            var alreadySent = await notificationDbContext.Notifications.AsNoTracking()
                .AnyAsync(n => n.OrderId == message.OrderId && n.Channel == "email", CancellationToken.None);
            if (!alreadySent)
            {
                throw;
            }

            return;
        }

        logger.LogInformation("EMAIL to customer {CustomerId} for order {OrderId}: \"{Subject}\" (simulated - stored, not sent).",
            message.CustomerId, message.OrderId, message.Subject);
    }
}
