using Sphere.Catalog.Domain;
using Sphere.Catalog.Features.Products;

namespace Sphere.Catalog.Tests;

public class ProductMapperTests
{
    [Fact]
    public void Copies_every_field_to_the_response()
    {
        var product = new Product
        {
            Id = Guid.CreateVersion7(),
            Sku = "SHP-101",
            Name = "Aurora Sneaker",
            Price = 89.90m,
            CategoryId = Guid.CreateVersion7(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        var response = product.ToResponse();

        Assert.Equal(product.Id, response.Id);
        Assert.Equal(product.Sku, response.Sku);
        Assert.Equal(product.Name, response.Name);
        Assert.Equal(product.Price, response.Price);
        Assert.Equal(product.CategoryId, response.CategoryId);
        Assert.Equal(product.CreatedAtUtc, response.CreatedAtUtc);
    }
}
