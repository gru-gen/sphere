using FluentValidation;
using Sphere.Ordering.Application.Pricing;

namespace Sphere.Ordering.Application.PlaceOrder;

internal sealed record PlaceOrderCommand(
    Guid EventId, Guid OrderId, Guid CustomerId, IReadOnlyList<PlaceOrderItem> Items) : IRequest;

internal sealed record PlaceOrderItem(Guid ProductId, int Quantity);

internal sealed class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderCommandValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
    }
}

internal sealed class PlaceOrderCommandHandler(
    IProductPriceReader prices,
    OrderingDbContext db,
    TimeProvider clock) : IRequestHandler<PlaceOrderCommand>
{
    private const string Currency = "EUR";

    public async Task Handle(PlaceOrderCommand command, CancellationToken ct)
    {
        var processed = await db.ProcessedEvents.AsNoTracking().AnyAsync(e => e.EventId == command.EventId, ct);
        if (processed)
        {
            return;
        }

        var priceMap = await prices.GetAsync(
            command.Items.Select(l => l.ProductId).ToArray(), ct);

        var lines = command.Items.Select(line =>
        {
            if (!priceMap.TryGetValue(line.ProductId, out var price))
            {
                throw new DomainException($"Product {line.ProductId} no longer exists.");
            }
            return (line.ProductId, price.Name,
                    Money.Of(price.Price, Currency), line.Quantity);
        }).ToList();

        var order = Order.Place(command.OrderId, command.CustomerId, lines, clock);
        db.Orders.Add(order);
        db.ProcessedEvents.Add(new ProcessedEvent
        {
            EventId = command.EventId,
            ProcessedAtUtc = clock.GetUtcNow(),
        });

        try
        {
            await db.SaveEntitiesAsync(ct);
        }
        catch (DbUpdateException)
        {
            var alreadyDone = await db.ProcessedEvents.AsNoTracking()
                .AnyAsync(e => e.EventId == command.EventId, CancellationToken.None);
            if (!alreadyDone)
            {
                throw;
            }
        }
    }
}
