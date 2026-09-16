namespace Sphere.Basket.Data;

internal sealed class CheckoutRecord
{
    public required string Key { get; init; }
    public Guid OrderId { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
}
