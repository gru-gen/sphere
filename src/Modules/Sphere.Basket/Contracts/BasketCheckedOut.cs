namespace Sphere.Basket.Contracts;

public sealed record BasketCheckedOut(
    Guid EventId,
    Guid CheckoutId,
    Guid CustomerId,
    IReadOnlyList<BasketCheckedOutLine> Lines,
    DateTimeOffset CheckedOutAtUtc);

public sealed record BasketCheckedOutLine(Guid ProductId, int Quantity);
