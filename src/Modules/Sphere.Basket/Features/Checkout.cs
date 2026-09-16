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
        Request request, HttpContext httpContext, IBasketStore store, IBasketEvents events,
        CancellationToken cancellationToken)
    {
        var key = httpContext.Request.Headers["Idempotency-Key"].FirstOrDefault();

        if (key is not null && await store.FindCheckoutAsync(key, cancellationToken) is { } seen)
        {
            return TypedResults.Accepted($"/api/orders/{seen}", new CheckoutAccepted(seen));
        }

        var orderId = Guid.CreateVersion7();

        BasketSnapshot snapshot;
        try
        {
            snapshot = await store.CheckoutAsync(request.CustomerId, key, orderId, cancellationToken);
        }
        catch (DbUpdateException) when (key is not null)
        {
            var winner = await store.FindCheckoutAsync(key, cancellationToken);
            return TypedResults.Accepted($"/api/orders/{winner}", new CheckoutAccepted(winner!.Value));
        }

        if (snapshot.Items.Count == 0)
        {
            return TypedResults.Problem(title: "The basket is empty.",
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        await events.PublishCheckedOutAsync(snapshot, orderId, cancellationToken);
        return TypedResults.Accepted($"/api/orders/{orderId}", new CheckoutAccepted(orderId));
    }
}
