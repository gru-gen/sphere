using Sphere.Ordering.Application.Events;

namespace Sphere.Integration.Tests.Ordering;

public sealed class RecordingOrderingEvents : IOrderingEvents
{
    public List<(Guid OrderId, Guid CustomerId, decimal Total)> Published { get; } = [];

    public Task EnsureTopicAsync() => Task.CompletedTask;

    public Task PublishOrderPlacedAsync(
        Guid orderId, Guid customerId, decimal total, string currency, CancellationToken ct)
    {
        Published.Add((orderId, customerId, total));
        return Task.CompletedTask;
    }
}
