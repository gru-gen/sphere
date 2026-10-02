namespace Sphere.Ordering.Domain.Abstract;

// summary: base for entities that raise domain events; the context collects them at save time.
internal abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected void PushDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public IReadOnlyList<IDomainEvent> PullDomainEvents()
    {
        // why: pull-and-clear — an event must never be dispatched twice.
        var events = _domainEvents.ToArray();

        _domainEvents.Clear();

        return events;
    }
}
