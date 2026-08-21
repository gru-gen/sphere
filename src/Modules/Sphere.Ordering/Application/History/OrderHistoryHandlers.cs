namespace Sphere.Ordering.Application.History;

internal sealed class OrderPlacedHistoryHandler(OrderingDbContext dbContext, TimeProvider timeProvider)
    : INotificationHandler<OrderPlacedDomainEvent>
{
    public Task Handle(OrderPlacedDomainEvent notification, CancellationToken cancellationToken)
    {
        dbContext.History.Add(new OrderHistoryEntry
        {
            OrderId = notification.OrderId,
            AtUtc = timeProvider.GetUtcNow(),
            What = $"Orders placed: {notification.Total} {notification.Currency}",
        });

        return Task.CompletedTask;
    }
}

internal sealed class OrderCancelledHistoryHandler(OrderingDbContext dbContext, TimeProvider timeProvider)
    : INotificationHandler<OrderCancelledDomainEvent>
{
    public Task Handle(OrderCancelledDomainEvent notification, CancellationToken cancellationToken)
    {
        dbContext.History.Add(new OrderHistoryEntry
        {
            OrderId = notification.OrderId,
            AtUtc = timeProvider.GetUtcNow(),
            What = "Orders cancelled by the customer",
        });

        return Task.CompletedTask;
    }
}
