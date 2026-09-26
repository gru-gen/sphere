namespace Sphere.Basket.Data;

// summary: one row per Idempotency-Key ever accepted at checkout — enough to
// replay the answer (the order's id) without clearing or announcing again.
internal sealed class CheckoutRecord
{
    public required string Key { get; init; }
    public Guid OrderId { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
}
