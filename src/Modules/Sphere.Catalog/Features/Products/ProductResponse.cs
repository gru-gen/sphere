namespace Sphere.Catalog.Features.Products;

internal sealed record ProductResponse(
    Guid Id,
    string Sku,
    string Name,
    decimal Price,
    Guid CategoryId,
    DateTimeOffset CreatedAtUtc);

internal sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int Total);
