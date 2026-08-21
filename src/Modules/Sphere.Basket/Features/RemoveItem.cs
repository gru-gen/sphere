using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Sphere.Basket.Features;

internal static class RemoveItem
{
    internal static async Task<NoContent> HandleAsync(
        Guid customerId, Guid productId, BasketDbContext dbContext, CancellationToken cancellationToken)
    {
        await dbContext.Items
            .Where(i => i.CustomerId == customerId && i.ProductId == productId)
            .ExecuteDeleteAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
