namespace Sphere.Ordering.Application.History;

// summary: the history trail — domain events become rows, in the same transaction.
internal sealed class OrderPlacedHistoryHandler(OrderingDbContext dbContext, TimeProvider clock)
    : INotificationHandler<OrderPlacedDomainEvent>
{
    public Task Handle(OrderPlacedDomainEvent notification, CancellationToken cancellationToken)
    {
        dbContext.OrderHistory.Add(new OrderHistoryEntry
        {
            OrderId = notification.OrderId,
            AtUtc = clock.GetUtcNow(),
            Message = $"Order placed: {notification.Total} {notification.Currency}",
        });

        return Task.CompletedTask;
    }
}
