namespace Sphere.Basket.Domain;

internal sealed class BasketItem
{
    public required Guid CustomerId { get; init; }
    public required Guid ProductId { get; init; }
    public required int Quantity { get; set; }
    public required DateTimeOffset UpdatedAtUtc { get; set; }
}
