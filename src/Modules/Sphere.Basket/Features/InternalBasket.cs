using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Sphere.Basket.Contracts;

namespace Sphere.Basket.Features;

// summary: the checkout-facing door. The same store as the public endpoints —
// a different AUDIENCE, so a separate, network-private surface.
internal static class InternalBasket
{
    internal static async Task<Ok<BasketSnapshot>> GetSnapshot(
        Guid customerId, IBasketStore basketStore, CancellationToken cancellationToken)
        => TypedResults.Ok(await basketStore.GetAsync(customerId, cancellationToken));

    internal static async Task<NoContent> Clear(
        Guid customerId, IBasketStore basketStore, CancellationToken cancellationToken)
    {
        // why: DELETE is idempotent by contract — clearing an already-empty
        // basket answers 204 just the same. Retries stay boring.
        await basketStore.ClearAsync(customerId, cancellationToken);
        return TypedResults.NoContent();
    }
}
