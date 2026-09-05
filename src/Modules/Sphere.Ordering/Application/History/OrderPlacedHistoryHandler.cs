namespace Sphere.Ordering.Application.History;

internal class OrderPlacedHistoryHandler(OrderingDbContext dbContext, TimeProvider clock)
    : INotificationHandler<OrderPlacedDomainEvent>
{
    public Task Handle(OrderPlacedDomainEvent notification, CancellationToken cancellationToken)
    {
        dbContext.History.Add(new OrderHistoryEntry
        {
            OrderId = notification.OrderId,
            AtUtc = clock.GetUtcNow(),
            What = $"Order placed: {notification.Total} {notification.Currency}",
        });

        return Task.CompletedTask;
    }
}
