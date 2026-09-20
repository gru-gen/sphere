using Riok.Mapperly.Abstractions;

namespace Sphere.Catalog.Features.Products;

[Mapper]
internal static partial class ProductMapper
{
    public static partial ProductResponse ToResponse(this Product product);
    public static partial IQueryable<ProductResponse> SelectToResponse(this IQueryable<Product> query);
}
