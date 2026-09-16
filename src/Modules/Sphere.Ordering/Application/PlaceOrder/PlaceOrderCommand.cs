using FluentValidation;
using Sphere.Ordering.Application.Pricing;

namespace Sphere.Ordering.Application.PlaceOrder;

internal sealed record PlaceOrderCommand(
    Guid OrderId, Guid CustomerId, IReadOnlyList<PlaceOrderLine> Lines) : IRequest;
internal sealed record PlaceOrderLine(Guid ProductId, int Quantity);

internal sealed class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty();
    }
}

internal sealed class PlaceOrderCommandHandler(
    IProductPriceReader productPriceReader,
    OrderingDbContext dbContext,
    TimeProvider clock) : IRequestHandler<PlaceOrderCommand>
{
    private const string Currency = "EUR";

    public async Task Handle(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        var priceMap = await productPriceReader.GetAsync(
            [.. command.Lines.Select(l => l.ProductId)], cancellationToken);

        var lines = command.Lines.Select(line =>
        {
            if (!priceMap.TryGetValue(line.ProductId, out var price))
            {
                throw new DomainException($"Product {line.ProductId} no longer exists.");
            }
            return (line.ProductId, price.Name,
                    Money.Of(price.Price, Currency), line.Quantity);
        }).ToList();

        var order = Order.Place(command.OrderId, command.CustomerId, lines, clock);
        dbContext.Orders.Add(order);
        await dbContext.SaveEntitiesAsync(cancellationToken);
    }
}
