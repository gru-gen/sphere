namespace Sphere.Ordering.Data;

// summary: one row per Idempotency-Key ever accepted — enough to replay the
// original checkout answer without redoing any of the work.
internal sealed class IdempotencyRecord
{
    public required string Key { get; init; }
    public Guid OrderId { get; init; }
    public decimal Total { get; init; }
    public string Currency { get; init; } = "";
    public DateTimeOffset CreatedAtUtc { get; init; }
}
