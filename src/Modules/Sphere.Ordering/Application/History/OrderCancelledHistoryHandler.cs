namespace Sphere.Ordering.Application.History;

internal sealed class OrderCancelledHistoryHandler(OrderingDbContext db, TimeProvider clock)
    : INotificationHandler<OrderCancelledDomainEvent>
{
    public Task Handle(OrderCancelledDomainEvent notification, CancellationToken ct)
    {
        db.OrderHistories.Add(new OrderHistoryEntry
        {
            OrderId = notification.OrderId,
            AtUtc = clock.GetUtcNow(),
            What = "Order cancelled by the customer",
        });

        return Task.CompletedTask;
    }
}
