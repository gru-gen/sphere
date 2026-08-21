namespace Sphere.Catalog.Contracts;

internal sealed class ProductPriceReader(CatalogDbContext dbContext) : IProductPriceReader
{
    public async Task<IReadOnlyDictionary<Guid, ProductPrice>> GetAsync(
        IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
    {
        var prices = await dbContext.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new ProductPrice(p.Id, p.Name, p.Price))
            .ToListAsync(cancellationToken);

        return prices.ToDictionary(p => p.ProductId);
    }
}
