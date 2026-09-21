namespace Sphere.Catalog.Contracts;

// summary: the Catalog module's public contract — current prices for a set of products.
public interface IProductPriceReader
{
    Task<IReadOnlyDictionary<Guid, ProductPrice>> GetAsync(
        IReadOnlyCollection<Guid> productsIds, CancellationToken cancellationToken);
}

public sealed record ProductPrice(Guid ProductId, string Name, decimal Price);
