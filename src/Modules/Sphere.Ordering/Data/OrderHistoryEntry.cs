namespace Sphere.Ordering.Data;

internal sealed class OrderHistoryEntry
{
    public long Id { get; init; }
    public required Guid OrderId { get; init; }
    public required DateTimeOffset AtUtc { get; init; }
    public required string What {  get; init; }
}
