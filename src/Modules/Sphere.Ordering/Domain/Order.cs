namespace Sphere.Ordering.Domain;

internal sealed class Order : Entity
{
    private readonly List<OrderLine> _lines = [];

    private Order() { }

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; } = null!;
    public string Currency { get; private set; } = null!;
    public decimal Total { get; private set; }
    public DateTimeOffset PlacedAtUtc { get; private set; }
    public IReadOnlyList<OrderLine> Lines => _lines;

    public void Cancel()
    {
        if (Status != OrderStatus.Placed)
        {
            throw new DomainException($"A {Status.Name} order cannot be cancelled.");
        }

        Status = OrderStatus.Cancelled;
        PushDomainEvent(new OrderCancelledDomainEvent(Id));
    }

    public static Order Place(
        Guid customerId,
        IReadOnlyList<(Guid ProductId, string Name, Money UnitPrice, int Quantity)> lines,
        TimeProvider timeProvider)
    {
        if (lines.Count == 0)
        {
            throw new DomainException("An order needs at least one line.");
        }

        if (lines.Any(l => l.Quantity is < 1 or > 100))
        {
            throw new DomainException("Line quantity must be between 1 and 100.");
        }

        var currency = lines[0].UnitPrice.Curreny;
        var total = lines
            .Select(l => l.UnitPrice * l.Quantity)
            .Aggregate((a, b) => a + b);

        var order = new Order
        {
            Id = Guid.CreateVersion7(),
            CustomerId = customerId,
            Status = OrderStatus.Placed,
            Currency = currency,
            Total = total.Amount,
            PlacedAtUtc = timeProvider.GetUtcNow()
        };

        order._lines.AddRange(
            lines.Select(l => new OrderLine(l.ProductId, l.Name, l.UnitPrice.Amount, l.Quantity)));

        order.PushDomainEvent(new OrderPlacedDomainEvent(order.Id, customerId, order.Total, currency));

        return order;
    }
}
