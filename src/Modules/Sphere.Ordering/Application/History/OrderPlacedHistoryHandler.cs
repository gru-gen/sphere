namespace Sphere.Ordering.Application.History;

// summary: the history trail — domain events become rows, in the same transaction.
internal sealed class OrderPlacedHistoryHandler(OrderingDbContext dbContext, TimeProvider clock)
    : INotificationHandler<OrderPlacedDomainEvent>
{
    public Task Handle(OrderPlacedDomainEvent notification, CancellationToken cancellationToken)
    {
        dbContext.OrderHistories.Add(new OrderHistoryEntry
        {
            OrderId = notification.OrderId,
            AtUtc = clock.GetUtcNow(),
            What = $"Order placed: {notification.Total} {notification.Currency}",
        });

        return Task.CompletedTask;
    }
}
