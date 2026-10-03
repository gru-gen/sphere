namespace Sphere.Ordering.Application.Checkout;

// summary: Ordering's OWN port for the one thing it needs from Catalog.
// why: the consumer owns the interface now — Catalog is a separate process,
// and Ordering decides what "a price" means on ITS side of the wire.
public interface IProductPriceReader
{
    Task<IReadOnlyDictionary<Guid, ProductPrice>> GetAsync(
        IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken);
}

public sealed record ProductPrice(Guid ProductId, string Name, decimal Price);
