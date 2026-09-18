namespace Sphere.Ordering.Application.Events;

internal interface IOrderingEvents
{
    Task EnsureTopicAsync();
    Task PublishOrderPlacedAsync(
        Guid orderId, Guid customerId, decimal total, string currency, CancellationToken cancellationToken);
}
