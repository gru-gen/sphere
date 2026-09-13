namespace Sphere.Basket.Contracts;

public sealed record BasketCheckedOut(
    Guid EventId, Guid CustomerId, IReadOnlyList<BasketCheckedOutLine> Lines, DateTimeOffset OccuredAtUtc);
public sealed record BasketCheckedOutLine(Guid ProductId, int Quantity);
