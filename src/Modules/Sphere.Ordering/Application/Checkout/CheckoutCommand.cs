using FluentValidation;
using Sphere.Basket.Contracts;

namespace Sphere.Ordering.Application.Checkout;

public sealed record CheckoutCommand(Guid CustomerId) : IRequest<CheckoutResult>;
public sealed record CheckoutResult(Guid OrderId, decimal Total, string Currency);

internal sealed class CheckoutCommandValidator : AbstractValidator<CheckoutCommand>
{
    public CheckoutCommandValidator() => RuleFor(x => x.CustomerId).NotEmpty();
}

// summary: the checkout use case — basket in, order out, basket cleared.
internal sealed class CheckoutCommandHandler(
    IBasketStore basketStore,
    IProductPriceReader productPriceReader,
    OrderingDbContext dbContext,
    TimeProvider clock) : IRequestHandler<CheckoutCommand, CheckoutResult>
{
    private const string Currency = "EUR";

    public async Task<CheckoutResult> Handle(CheckoutCommand command, CancellationToken ct)
    {
        var basket = await basketStore.GetAsync(command.CustomerId, ct);
        if (basket.Items.Count == 0)
        {
            throw new DomainException("The basket is empty.");
        }

        // why: prices come from the catalog AT CHECKOUT — never from the client.
        var priceMap = await productPriceReader.GetAsync(
            [.. basket.Items.Select(i => i.ProductId)], ct);

        var lines = basket.Items.Select(item =>
        {
            if (!priceMap.TryGetValue(item.ProductId, out var price))
            {
                throw new DomainException($"Product {item.ProductId} no longer exists.");
            }
            return (item.ProductId, price.Name, Money.Of(price.Price, Currency), item.Quantity);
        }).ToList();

        var order = Order.Place(command.CustomerId, lines, clock);
        dbContext.Orders.Add(order);
        await dbContext.SaveEntitiesAsync(ct);

        // tradeoff: a second, separate commit (ADR-005). If the clear fails, the order
        // exists and the basket lingers.
        await basketStore.ClearAsync(command.CustomerId, ct);

        return new CheckoutResult(order.Id, order.Total, order.Currency);
    }
}
