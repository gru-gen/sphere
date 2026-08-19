using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Sphere.Catalog.Features.Categories;

internal static class GetCategories
{
    internal sealed record CategoryResponse(Guid Id, string Name);

    internal static async Task<Ok<List<CategoryResponse>>> HandleAsync(
        CatalogDbContext dbContext, CancellationToken cancellationToken)
    {
        var categories = await dbContext.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponse(c.Id, c.Name))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(categories);
    }
}
