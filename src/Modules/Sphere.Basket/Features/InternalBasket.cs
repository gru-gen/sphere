using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Sphere.Basket.Contracts;

namespace Sphere.Basket.Features;

internal static class InternalBasket
{
    internal static async Task<Ok<BasketSnapshot>> GetBasketHandle(
        Guid customerId, IBasketStore basketStore, CancellationToken cancellationToken)
        => TypedResults.Ok(await basketStore.GetAsync(customerId, cancellationToken));

    internal static async Task<NoContent> ClearHandle(
        Guid customerId, IBasketStore basketStore, CancellationToken cancellationToken)
    {
        await basketStore.ClearAsync(customerId, cancellationToken);
        return TypedResults.NoContent();
    }
}
