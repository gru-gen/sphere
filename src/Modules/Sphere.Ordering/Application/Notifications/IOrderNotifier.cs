namespace Sphere.Ordering.Application.Notifications;

internal interface IOrderNotifier
{
    Task SendConfirmationAsync(
        Guid orderId, Guid customerId, decimal total, string currency, CancellationToken cancellationToken);
}
