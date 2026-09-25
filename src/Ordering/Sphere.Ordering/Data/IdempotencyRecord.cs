namespace Sphere.Ordering.Data;

internal sealed class IdempotencyRecord
{
    public required string Key { get; init; }
    public Guid OrderId { get; init; }
    public decimal Total { get; init; }
    public string Currency { get; init; } = "";
    public DateTimeOffset CreatedAtUtc { get; init; }
}
