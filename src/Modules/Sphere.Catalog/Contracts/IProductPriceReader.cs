namespace Sphere.Catalog.Contracts;

public interface IProductPriceReader
{
    Task<IReadOnlyDictionary<Guid, ProductPrice>> GetAsync(
        IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken);
}

public sealed record ProductPrice(Guid ProductId, string Name, decimal Price);
