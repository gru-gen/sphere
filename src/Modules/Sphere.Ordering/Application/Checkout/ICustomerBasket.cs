namespace Sphere.Ordering.Application.Checkout;

// summary: Ordering's OWN port for the two things checkout needs from Basket —
// read the snapshot, clear it after the order commits.
// Basket is a separate process now, and Ordering decides what
// "a basket" means on ITS side of the wire.
public interface ICustomerBasket
{
    Task<CustomerBasket> GetAsync(Guid customerId, CancellationToken cancellationToken);
    Task ClearAsync(Guid customerId, CancellationToken cancellationToken);
}

public sealed record CustomerBasket(
    Guid CustomerId, IReadOnlyList<CustomerBasketLine> Lines);
public sealed record CustomerBasketLine(Guid ProductId, int Quantity);
