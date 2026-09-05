using Sphere.Ordering.Domain;
using Sphere.Ordering.Domain.Abstract;
using Sphere.Ordering.Domain.Events;

namespace Sphere.Ordering.Tests;

public class OrderTests
{
    private static readonly TimeProvider Clock = Substitute.For<TimeProvider>();

    private static Order PlaceOne() => Order.Place(
        Guid.CreateVersion7(),
        [(Guid.CreateVersion7(), "Aurora Sneaker", Money.Of(89.90m, "EUR"), 2)],
        Clock);

    [Fact]
    public void Place_computes_the_total_and_raises_the_event()
    {
        var order = PlaceOne();

        Assert.Equal(179.80m, order.Total);
        Assert.Equal(OrderStatus.Placed, order.Status);
        var domainEvent = Assert.Single(order.PullEvents());
        Assert.IsType<OrderPlacedDomainEvent>(domainEvent);
    }

    [Fact]
    public void Place_refuses_an_empty_order()
    {
        Assert.Throws<DomainException>(() => Order.Place(Guid.NewGuid(), [], Clock));
    }

    [Fact]
    public void Place_refuses_a_bad_quantity()
    {
        Assert.Throws<DomainException>(() => Order.Place(
            Guid.NewGuid(),
            [(Guid.NewGuid(), "Aurora", Money.Of(10m, "EUR"), 0)],
            Clock));
    }

    [Fact]
    public void Cancel_works_once_and_only_from_placed()
    {
        var order = PlaceOne();
        order.PullEvents();

        order.Cancel();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.IsType<OrderCancelledDomainEvent>(Assert.Single(order.PullEvents()));
        Assert.Throws<DomainException>(order.Cancel);
    }

    [Fact]
    public void Events_are_pulled_only_once()
    {
        var order = PlaceOne();

        Assert.Single(order.PullEvents());
        Assert.Empty(order.PullEvents());
    }
}
