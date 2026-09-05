namespace Sphere.Ordering.Application.History;

internal class OrderCancelledHistoryHandler(OrderingDbContext dbContext, TimeProvider clock)
    : INotificationHandler<OrderCancelledDomainEvent>
{
    public Task Handle(OrderCancelledDomainEvent notification, CancellationToken cancellationToken)
    {
        dbContext.History.Add(new OrderHistoryEntry
        {
            OrderId = notification.OrderId,
            AtUtc = clock.GetUtcNow(),
            What = "Order cancelled by the customer",
        });

        return Task.CompletedTask;
    }
}
