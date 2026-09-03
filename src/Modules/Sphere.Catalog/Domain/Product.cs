namespace Sphere.Catalog.Domain;

internal sealed class Product
{
    public required Guid Id { get; init; }
    public required string Sku {  get; init; }
    public required string Name { get; set; }
    public required decimal Price { get; set; }
    public required Guid CategoryId { get; set; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
}
