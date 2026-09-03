using Sphere.Catalog.Features.Products;

namespace Sphere.Catalog.Tests;

public class CreateProductFactoryTests
{
    [Fact]
    public void Stamps_creation_time_from_the_clock_not_the_wall()
    {
        var frozen = new DateTimeOffset(2026, 8, 9, 12, 0, 0, TimeSpan.Zero);
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(frozen);

        var request = new CreateProduct.Request("SHP-101", "Aurora", 89.90m, Guid.CreateVersion7());
        var product = CreateProduct.ToProduct(request, clock);

        Assert.Equal(frozen, product.CreatedAtUtc);
        Assert.Equal(7, product.Id.Version);
    }
}
