namespace Sphere.Basket.Contracts;

public interface IBasketStore
{
    Task<BasketSnapshot> GetAsync(Guid customerId, CancellationToken cancellationToken);

    Task ClearAsync(Guid customerId, CancellationToken cancellationToken);
}

public sealed record BasketSnapshot(Guid CustomerId, IReadOnlyList<BasketSnapshotItem> Items);
public sealed record BasketSnapshotItem(Guid ProductId, int Quantity);
