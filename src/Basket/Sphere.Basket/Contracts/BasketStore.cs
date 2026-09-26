namespace Sphere.Basket.Contracts;

internal class BasketStore(BasketDbContext dbContext, TimeProvider clock) : IBasketStore
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

    public async Task<Guid?> FindCheckoutAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        var record = await dbContext.Checkouts.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Key == idempotencyKey, cancellationToken);
        return record?.OrderId;
    }

    public async Task<BasketSnapshot> CheckoutAsync(Guid customerId, string? idempotencyKey, Guid orderId, CancellationToken cancellationToken)
    {
        var items = await dbContext.Items
            .Where(i => i.CustomerId == customerId)
            .OrderBy(i => i.ProductId)
            .ToListAsync(cancellationToken);

        var snapshot = new BasketSnapshot(customerId,
            items.Select(i => new BasketSnapshotItem(i.ProductId, i.Quantity)).ToList());

        if (items.Count == 0)
        {
            return snapshot;
        }

        dbContext.Items.RemoveRange(items);
        if (idempotencyKey is not null)
        {
            dbContext.Checkouts.Add(new CheckoutRecord
            {
                Key = idempotencyKey,
                OrderId = orderId,
                CreatedAtUtc = clock.GetUtcNow(),
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return snapshot;
    }
}
