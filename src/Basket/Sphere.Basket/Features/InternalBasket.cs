using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Sphere.Basket.Contracts;

namespace Sphere.Basket.Features;

internal static class InternalBasket
{
    internal static async Task<Ok<BasketSnapshot>> GetSnapshot(
        Guid customerId, IBasketStore basketStore, CancellationToken cancellationToken)
        => TypedResults.Ok(await basketStore.GetAsync(customerId, cancellationToken));

    internal static async Task<NoContent> Clear(
        Guid customerId, IBasketStore basketStore, IBasketEvents basketEvents, CancellationToken cancellationToken)
    {
        var snapshot = await basketStore.GetAsync(customerId, cancellationToken);

        await basketStore.ClearAsync(customerId, cancellationToken);

        if (snapshot.Items.Count > 0)
        {
            await basketEvents.PublishCheckedOutAsync(snapshot, cancellationToken);
        }

        return TypedResults.NoContent();
    }
}
