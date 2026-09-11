using FluentValidation;

namespace Sphere.Ordering.Application.Checkout;

public sealed record CheckoutCommand(Guid CustomerId, string? IdempotencyKey = null) : IRequest<CheckoutResult>;
public sealed record CheckoutResult(Guid OrderId, decimal Total, string Currency);

internal sealed class CheckoutCommandValidator : AbstractValidator<CheckoutCommand>
{
    public CheckoutCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).MaximumLength(200);
    }
}

// summary: the checkout use case — basket in, order out, basket cleared.
internal sealed class CheckoutCommandHandler(
    ICustomerBasket customerBasket,
    IProductPriceReader productPriceReader,
    OrderingDbContext dbContext,
    TimeProvider clock) : IRequestHandler<CheckoutCommand, CheckoutResult>
{
    private const string Currency = "EUR";

    public async Task<CheckoutResult> Handle(CheckoutCommand command, CancellationToken cancellationToken)
    {
        if (command.IdempotencyKey is { } key)
        {
            var seen = await dbContext.IdempotencyRecords.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Key == key, cancellationToken);
            if (seen is not null)
            {
                return new CheckoutResult(seen.OrderId, seen.Total, seen.Currency);
            }
        }

        var basket = await customerBasket.GetAsync(command.CustomerId, cancellationToken);
        if (basket.Lines.Count == 0)
        {
            throw new DomainException("The basket is empty.");
        }

        var priceMap = await productPriceReader.GetAsync(
            [.. basket.Lines.Select(i => i.ProductId)], cancellationToken);

        var lines = basket.Lines.Select(item =>
        {
            if (!priceMap.TryGetValue(item.ProductId, out var price))
            {
                throw new DomainException($"Product {item.ProductId} no longer exists.");
            }

            return (item.ProductId, price.Name, Money.Of(price.Price, Currency), item.Quantity);
        }).ToList();

        var order = Order.Place(command.CustomerId, lines, clock);
        dbContext.Orders.Add(order);

        if (command.IdempotencyKey is { } newKey)
        {
            dbContext.IdempotencyRecords.Add(new IdempotencyRecord
            {
                Key = newKey,
                OrderId = order.Id,
                Total = order.Total,
                Currency = order.Currency,
                CreatedAtUtc = clock.GetUtcNow()
            });
        }

        try
        {
            await dbContext.SaveEntitiesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (command.IdempotencyKey is not null)
        {
            var winner = await dbContext.IdempotencyRecords.AsNoTracking()
                .FirstAsync(r => r.Key == command.IdempotencyKey, cancellationToken);
            return new CheckoutResult(winner.OrderId, winner.Total, winner.Currency);
        }

        // tradeoff: a second, separate commit.
        await customerBasket.ClearAsync(command.CustomerId, cancellationToken);

        return new CheckoutResult(order.Id, order.Total, order.Currency);
    }
}
