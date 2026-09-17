using MassTransit;
using Sphere.Notification.Service.Messages;

namespace Sphere.Notification.Service.Consumers;

internal sealed class SendOrderConfirmationConsumer(
    ILogger<SendOrderConfirmationConsumer> logger) : IConsumer<SendOrderConfirmation>
{
    public async Task Consume(ConsumeContext<SendOrderConfirmation> context)
    {
        var command = context.Message;
        var subject = $"Your sphere order is placed";
        var body =
            $"Thank you! Order {command.OrderId} was placed for a total of " +
            $"{command.Total} {command.Currency}. We will let you know when it ships.";

        logger.LogInformation("Confirmation for order {OrderId} rendered; fanning out.",
            command.OrderId);

        await context.Publish(new OrderConfirmationReady(
            command.OrderId, command.CustomerId, subject, body));
    }
}
