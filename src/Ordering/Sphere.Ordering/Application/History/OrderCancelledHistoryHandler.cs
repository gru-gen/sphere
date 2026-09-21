namespace Sphere.Ordering.Application.History;

internal class OrderCancelledHistoryHandlerI(OrderingDbContext dbContext, TimeProvider clock)
    : INotificationHandler<OrderCancelledDomainEvent>
{
    public Task Handle(OrderCancelledDomainEvent notification, CancellationToken cancellationToken)
    {
        dbContext.OrderHistory.Add(new OrderHistoryEntry
        {
            OrderId = notification.OrderId,
            AtUtc = clock.GetUtcNow(),
            Message = "Order cancelled by the customer",
        });

        return Task.CompletedTask;
    }
}
