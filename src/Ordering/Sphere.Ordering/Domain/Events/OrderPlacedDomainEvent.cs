namespace Sphere.Ordering.Domain.Events;

public sealed record OrderPlacedDomainEvent(
    Guid OrderId, Guid CustomerId, decimal Total, string Currency) : IDomainEvent;
