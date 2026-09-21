namespace Sphere.Ordering.Domain.Events;

public sealed record OrderCancelledDomainEvent(Guid OrderId) : IDomainEvent;
