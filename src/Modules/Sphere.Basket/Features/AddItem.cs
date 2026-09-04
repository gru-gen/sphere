using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Sphere.Basket.Features;

internal static class AddItem
{
    internal sealed record Request(Guid ProductId, int Quantity);

    internal sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.ProductId).NotEmpty();
            RuleFor(x => x.Quantity).InclusiveBetween(1, 100);
        }
    }

    internal static async Task<NoContent> Handle(
        Guid customerId, Request request, BasketDbContext dbContext,
        TimeProvider clock, CancellationToken cancellationToken)
    {
        var item = await dbContext.Items.FindAsync([customerId, request.ProductId], cancellationToken);
        if (item is null)
        {
            dbContext.Items.Add(new BasketItem
            {
                CustomerId = customerId,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                UpdatedAtUtc = clock.GetUtcNow()
            });
        }
        else
        {
            // why: adding the same product again means "more of it", not a duplicate row.
            item.Quantity = Math.Min(item.Quantity + request.Quantity, 100);
            item.UpdatedAtUtc = clock.GetUtcNow();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }
}
