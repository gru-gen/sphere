namespace Sphere.Catalog.Contracts;

// summary: in-process implementation of the price contract; internal on purpose.
internal sealed class ProductPriceReader(CatalogDbContext dbContext) : IProductPriceReader
{
    public async Task<IReadOnlyDictionary<Guid, ProductPrice>> GetAsync(IReadOnlyCollection<Guid> productsIds,
        CancellationToken cancellationToken)
    {

        var prices = await dbContext.Products.AsNoTracking()
            .Where(p => productsIds.Contains(p.Id))
            .Select(p => new ProductPrice(p.Id, p.Name, p.Price))
            .ToListAsync(cancellationToken);

        return prices.ToDictionary(p => p.ProductId);
    }
}
