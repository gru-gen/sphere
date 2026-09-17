using MassTransit;
using Sphere.Ordering.Application.Notifications;
using Sphere.Ordering.Contracts.Messages;

namespace Sphere.Ordering.Infrastructure;

internal sealed class MassTransitOrderNotifier(
    ISendEndpointProvider sendEndpoints) : IOrderNotifier
{
    private static readonly Uri Queue = new("queue:send-order-confirmation");

    public async Task SendConfirmationAsync(
        Guid orderId, Guid customerId, decimal total, string currency, CancellationToken cancellationToken)
    {
        var endpoint = await sendEndpoints.GetSendEndpoint(Queue);
        await endpoint.Send(new SendOrderConfirmation(orderId, customerId, total, currency), cancellationToken);
    }
}
