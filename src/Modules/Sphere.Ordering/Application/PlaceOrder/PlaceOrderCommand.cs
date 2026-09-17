using FluentValidation;
using Sphere.Ordering.Application.Notifications;
using Sphere.Ordering.Application.Pricing;

namespace Sphere.Ordering.Application.PlaceOrder;

internal sealed record PlaceOrderCommand(
    Guid EventId, Guid OrderId, Guid CustomerId, IReadOnlyList<PlaceOrderLine> Lines) : IRequest;
internal sealed record PlaceOrderLine(Guid ProductId, int Quantity);

internal sealed class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderCommandValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty();
    }
}

internal sealed class PlaceOrderCommandHandler(
    IProductPriceReader productPriceReader,
    OrderingDbContext dbContext,
    TimeProvider clock,
    IOrderNotifier notifier) : IRequestHandler<PlaceOrderCommand>
{
    private const string Currency = "EUR";

    public async Task Handle(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        var eventProcessed = await dbContext.ProcessedEvents.AsNoTracking()
            .AnyAsync(e => e.EventId == command.EventId, cancellationToken);
        if (eventProcessed)
        {
            return;
        }

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
        dbContext.ProcessedEvents.Add(new ProcessedEvent
        {
            EventId = command.EventId,
            ProcessedAtUtc = clock.GetUtcNow(),
        });

        try
        {
            await dbContext.SaveEntitiesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            var alreadyDone = await dbContext.ProcessedEvents.AsNoTracking()
                .AnyAsync(e => e.EventId == command.EventId, cancellationToken);
            if (!alreadyDone)
            {
                throw;
            }

            return;
        }

        await notifier.SendConfirmationAsync(order.Id, order.CustomerId, order.Total, order.Currency, cancellationToken);
    }
}
