using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Sphere.Basket.Contracts;

namespace Sphere.Basket.Features;

internal static class Checkout
{
    internal sealed record Request(Guid CustomerId);
    internal sealed record CheckoutAccepted(Guid OrderId);

    internal sealed class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(x => x.CustomerId).NotEmpty();
        }
    }

    internal static async Task<Results<Accepted<CheckoutAccepted>, ProblemHttpResult>> Handle(
        Request request, HttpContext httpContext, IBasketStore basketStore, IBasketEvents basketEvents,
        CancellationToken cancellationToken)
    {
        var key = httpContext.Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (key is not null && await basketStore.FindCheckoutAsync(key, cancellationToken) is { } seen)
        {
            return TypedResults.Accepted($"/api/orders/{seen}", new CheckoutAccepted(seen));
        }

        var orderId = Guid.CreateVersion7();

        BasketSnapshot basketSnapshot;
        try
        {
            basketSnapshot = await basketStore.CheckoutAsync(request.CustomerId, key, orderId, cancellationToken);
        }
        catch (DbUpdateException) when (key is not null)
        {
            // why: two racers, one fence — the loser's whole transaction
            // (clear included) rolled back on the duplicate key. Hand back
            // the winner's answer instead of a second checkout.
            var winner = await basketStore.FindCheckoutAsync(key, cancellationToken);
            return TypedResults.Accepted($"/api/orders/{winner}", new CheckoutAccepted(winner!.Value));
        }

        if (basketSnapshot.Items.Count == 0)
        {
            return TypedResults.Problem(title: "The basket is empty.",
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        await basketEvents.PublishCheckedOutAsync(basketSnapshot, orderId, cancellationToken);
        return TypedResults.Accepted($"/api/orders/{orderId}", new CheckoutAccepted(orderId));
    }
}
