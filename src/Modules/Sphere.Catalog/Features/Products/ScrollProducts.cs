using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Sphere.Catalog.Features.Products;

// summary: keyset pagination — "give me the next page AFTER this row" instead of
// "skip N rows". Depth costs nothing because the b-tree is entered, not walked.
internal static class ScrollProducts
{
    internal sealed record Request(string? AfterName = null, Guid? AfterId = null, int PageSize = 20);

    internal sealed record ScrollResponse(IReadOnlyList<ProductResponse> Items,
        string? NextAfterName, Guid? NextAfterId);

    internal static async Task<Ok<ScrollResponse>> Handle(
        [AsParameters] Request request, CatalogDbContext dbContext, CancellationToken cancellationToken)
    {
        var size = Math.Clamp(request.PageSize, 1, 100);
        var query = dbContext.Products.AsNoTracking();

        if (request is { AfterName: { } afterName, AfterId: { } afterId })
        {
            // why: "(name, id) greater than the cursor" — written as the OR-form the
            // provider translates; it walks ix_products_name_id from that exact spot.
            query = query.Where(p => p.Name.CompareTo(afterName) > 0 ||
                (p.Name == afterName && p.Id.CompareTo(afterId) > 0));
        }

        var items = await query
            .OrderBy(p => p.Name).ThenBy(p => p.Id)
            .Take(size)
            .ProjectToResponse()
            .ToListAsync(cancellationToken);

        var last = items.Count == size ? items[^1] : null;
        return TypedResults.Ok(new ScrollResponse(items, last?.Name, last?.Id));
    }
}
