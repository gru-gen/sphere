using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Sphere.Catalog.Features.Internal;

internal static class GetPrices
{
    internal sealed record Request(IReadOnlyCollection<Guid> ProductIds);
    internal sealed record PriceLine(Guid ProductId, string Name, decimal Price);

    internal static async Task<Ok<List<PriceLine>>> Handle(
        Request request, CatalogDbContext dbContext, CancellationToken cancellationToken)
    {
        var prices = await dbContext.Products.AsNoTracking()
            .Where(p => request.ProductIds.Contains(p.Id))
            .Select(p => new PriceLine(p.Id, p.Name, p.Price))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(prices);
    }
}
