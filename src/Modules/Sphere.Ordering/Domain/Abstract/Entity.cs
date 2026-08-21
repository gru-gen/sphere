namespace Sphere.Ordering.Domain.Abstract;

internal abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected void PushDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public IReadOnlyList<IDomainEvent> PullDomainEvents()
    {
        var events = _domainEvents.ToArray();
        _domainEvents.Clear();
        return events;
    }
}
