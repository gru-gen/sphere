using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Sphere.Catalog.Features.Products;

internal static class GetProductById
{
    internal static async Task<Results<Ok<ProductResponse>, NotFound>> Handle(
        Guid id, CatalogDbContext dbContext, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return product is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(product.ToResponse());
    }
}
