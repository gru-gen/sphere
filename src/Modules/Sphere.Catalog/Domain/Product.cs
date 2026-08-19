namespace Sphere.Catalog.Domain;

internal sealed class Product
{
    public required Guid Id { get; init; }
    public required string Sku {  get; init; }
    public required string Name { get; init; }
    public required decimal Price { get; init; }
    public required Guid CategoryId { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
}
