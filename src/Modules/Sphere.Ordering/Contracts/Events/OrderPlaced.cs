namespace Sphere.Ordering.Contracts.Events;

public sealed record OrderPlaced(
    Guid EventId,
    Guid OrderId,
    Guid CustomerId,
    decimal Total,
    string Currency,
    DateTimeOffset PlacedAtUtc);
