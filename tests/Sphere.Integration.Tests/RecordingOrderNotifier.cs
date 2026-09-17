using Sphere.Ordering.Application.Notifications;

namespace Sphere.Integration.Tests;

public sealed class RecordingOrderNotifier : IOrderNotifier
{
    public List<(Guid OrderId, Guid CustomerId, decimal Total, string Currency)> Sent { get; } = [];

    public Task SendConfirmationAsync(
        Guid orderId, Guid customerId, decimal total, string currency, CancellationToken ct)
    {
        Sent.Add((orderId, customerId, total, currency));
        return Task.CompletedTask;
    }
}
