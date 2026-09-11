namespace Sphere.Ordering.Application.Checkout;

public interface ICustomerBasket
{
    Task<CustomerBasket> GetAsync(Guid customerId, CancellationToken cancellationToken);
    Task ClearAsync(Guid customerId, CancellationToken cancellationToken);
}

public sealed record CustomerBasket(Guid CustomerId, IReadOnlyList<CustomerBasketLine> Lines);
public sealed record CustomerBasketLine(Guid ProductId, int Quantity);
