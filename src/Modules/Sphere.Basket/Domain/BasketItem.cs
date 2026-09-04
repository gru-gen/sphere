namespace Sphere.Basket.Domain;

// summary: one row per (customer, product); a basket IS its rows — no parent table.
internal sealed class BasketItem
{
    public required Guid CustomerId { get; init; }
    public required Guid ProductId { get; init; }
    public required int Quantity { get; set; }
    public required DateTimeOffset UpdatedAtUtc { get; set; }
}
