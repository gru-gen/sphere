using Sphere.Ordering.Domain;

namespace Sphere.Ordering.Tests;

public class OrderStatusTests
{
    [Theory]
    [InlineData(1, "Placed")]
    [InlineData(2, "Cancelled")]
    public void Maps_ids_to_named_values(int id, string name)
    {
        Assert.Equal(name, OrderStatus.FromId(id).Name);
    }

    [Fact]
    public void Rejects_an_unknown_id()
    {
        Assert.Throws<DomainException>(() => OrderStatus.FromId(42));
    }
}
