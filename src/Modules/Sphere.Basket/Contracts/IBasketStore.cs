namespace Sphere.Basket.Contracts;

// summary: the Basket module's public contract — read a snapshot, and check
// out: clear the rows and remember the key, in one transaction.
public interface IBasketStore
{
    Task<BasketSnapshot> GetAsync(Guid customerId, CancellationToken cancellationToken);
    Task<Guid?> FindCheckoutAsync(string idempotencyKey, CancellationToken cancellationToken);
    Task<BasketSnapshot> CheckoutAsync(Guid customerId, string? idempotencyKey, Guid orderId, CancellationToken cancellationToken);
}

public sealed record BasketSnapshot(Guid CustomerId, IReadOnlyList<BasketSnapshotItem> Items);
public sealed record BasketSnapshotItem(Guid ProductId, int Quantity);
