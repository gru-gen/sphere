namespace Sphere.Ordering.Contracts.Messages;

public sealed record SendOrderConfirmation(
    Guid OrderId,
    Guid CustomerId,
    decimal Total,
    string Currency);
