namespace Sphere.Notification.Service.Messages;

public sealed record SendOrderConfirmation(
    Guid OrderId,
    Guid CustomerId,
    decimal Total,
    string Currency);
