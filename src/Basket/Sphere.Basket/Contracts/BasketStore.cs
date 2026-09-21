namespace Sphere.Basket.Contracts;

internal class BasketStore(BasketDbContext dbContext) : IBasketStore
{
    public async Task<BasketSnapshot> GetAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var items = await dbContext.Items.AsNoTracking()
            .Where(i => i.CustomerId == customerId)
            .OrderBy(i => i.ProductId)
            .Select(i => new BasketSnapshotItem(i.ProductId, i.Quantity))
            .ToListAsync(cancellationToken);

        return new BasketSnapshot(customerId, items);
    }

    public async Task ClearAsync(Guid customerId, CancellationToken cancellationToken)
    {
        // why: a set-based delete — no loading rows just to remove them.
        await dbContext.Items.Where(i => i.CustomerId == customerId).ExecuteDeleteAsync(cancellationToken);
    }
}
