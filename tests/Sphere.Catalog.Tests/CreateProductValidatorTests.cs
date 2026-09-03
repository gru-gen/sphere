using Sphere.Catalog.Features.Products;

namespace Sphere.Catalog.Tests;

public class CreateProductValidatorTests
{
    private readonly CreateProduct.Validator _validator = new();

    private static CreateProduct.Request Valid() =>
        new("SHP-101", "Aurora Sneaker", 89.90m, Guid.CreateVersion7());

    [Fact]
    public void Accepts_a_valid_request()
    {
        var result = _validator.Validate(Valid());
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("shp-101")]
    [InlineData("SHP 101")]
    public void Rejects_a_bad_sku(string sku)
    {
        var result = _validator.Validate(Valid() with { Sku = sku });
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1_000_000)]
    public void Rejects_a_bad_price(decimal price)
    {
        var result = _validator.Validate(Valid() with { Price = price });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Rejects_an_empty_category()
    {
        var result = _validator.Validate(Valid() with { CategoryId = Guid.Empty });
        Assert.False(result.IsValid);
    }
}
