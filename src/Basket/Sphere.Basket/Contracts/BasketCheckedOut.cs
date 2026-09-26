namespace Sphere.Basket.Contracts;

public sealed record BasketCheckedOut(
    Guid EventId,
    Guid CheckoutId,
    Guid CustomerId,
    IReadOnlyList<BasketCheckedOutItem> Items,
    DateTimeOffset OccurredAtUtc);

public sealed record BasketCheckedOutItem(Guid ProductId, int Quantity);
