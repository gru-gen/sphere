using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Sphere.Catalog.Features.Products;

internal static class GetProducts
{
    internal sealed record Request(int Page = 1, int PageSize = 20, Guid? CategoryId = null);

    internal static async Task<Ok<PagedResponse<ProductResponse>>> Handle(
        [AsParameters] Request request, CatalogDbContext dbContext, CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var size = Math.Clamp(request.PageSize, 1, 100);

        var query = dbContext.Products.AsNoTracking();
        if (request.CategoryId is { } categoryId)
        {
            query = query.Where(p => p.CategoryId == categoryId);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.Name).ThenBy(p => p.Id)
            .Skip((page - 1) * size).Take(size)
            .ProjectToResponse()
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new PagedResponse<ProductResponse>(items, page, size, total));
    }
}
