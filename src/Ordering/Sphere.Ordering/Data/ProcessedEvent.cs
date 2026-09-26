namespace Sphere.Ordering.Data;

// summary: the INBOX — one row per event this module has fully processed.
// The consumer's memory, kept where the consumer's work commits: the database.
internal sealed class ProcessedEvent
{
    public Guid EventId { get; init; }
    public DateTimeOffset ProcessedAtUtc { get; init; }
}
