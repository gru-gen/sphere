using Sphere.Ordering.Domain;
using Sphere.Ordering.Domain.Abstract;

namespace Sphere.Ordering.Tests;

public class MoneyTests
{
    [Fact]
    public void Adds_same_currency()
    {
        var sum = Money.Of(10.50m, "EUR") + Money.Of(2.25m, "EUR");
        Assert.Equal(12.75m, sum.Amount);
    }

    [Fact]
    public void Refuses_mixed_currencies()
    {
        Assert.Throws<DomainException>(
            () => Money.Of(10m, "EUR") + Money.Of(10m, "USD"));
    }

    [Fact]
    public void Refuses_negative_amounts()
    {
        Assert.Throws<DomainException>(() => Money.Of(-1m, "EUR"));
    }

    [Fact]
    public void Multiplies_by_quantity()
    {
        Assert.Equal(29.97m, (Money.Of(9.99m, "EUR") * 3).Amount);
    }
}
