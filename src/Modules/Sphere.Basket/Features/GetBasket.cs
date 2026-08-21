using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Sphere.Basket.Contracts;

namespace Sphere.Basket.Features;

internal static class GetBasket
{
    internal static async Task<Ok<BasketSnapshot>> HandleAsync(
        Guid customerId, IBasketStore basketStore, CancellationToken cancellationToken)
        => TypedResults.Ok(await basketStore.GetAsync(customerId, cancellationToken));
}
