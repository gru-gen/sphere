namespace Sphere.Basket.Contracts;

public interface IBasketStore
{
    Task<BasketSnapshot> GetAsync(Guid customerId, CancellationToken cancellationToken);
    Task<Guid?> FindCheckoutAsync(string idempotencyKey, CancellationToken cancellationToken);
    Task<BasketSnapshot> CheckoutAsync(Guid customerId, string? idempotencyKey, Guid orderId, CancellationToken cancellationToken);
}

public sealed record BasketSnapshot(Guid CustomerId, IReadOnlyList<BasketSnapshotItem> Items);
public sealed record BasketSnapshotItem(Guid ProductId, int Quantity);
