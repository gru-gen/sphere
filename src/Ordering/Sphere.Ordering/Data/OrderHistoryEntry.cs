namespace Sphere.Ordering.Data;

// summary: the order's paper trail — one row per meaningful fact, append-only.
internal sealed class OrderHistoryEntry
{
    public long Id { get; init; }
    public required Guid OrderId { get; init; }
    public required DateTimeOffset AtUtc { get; init; }
    public required string Message { get; init; }
}
