namespace Sphere.Ordering.Data;

internal sealed class ProcessedEvent
{
    public Guid EventId { get; init; }
    public DateTimeOffset ProcessedAtUtc { get; init; }
}
