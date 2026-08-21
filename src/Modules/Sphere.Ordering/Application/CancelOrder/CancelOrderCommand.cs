namespace Sphere.Ordering.Application.CancelOrder;

public sealed record CancelOrderCommand(Guid OrderId) : IRequest;

internal sealed class CancelOrderCommandHandler(OrderingDbContext dbContext)
    : IRequestHandler<CancelOrderCommand>
{
    public async Task Handle(CancelOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.Id == command.OrderId, cancellationToken)
            ?? throw new DomainException($"Order {command.OrderId} was not found.");

        order.Cancel();
        await dbContext.SaveEntitiesAsync(cancellationToken);
    }
}
