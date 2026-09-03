namespace Sphere.Catalog.Data;

internal static class CatalogSeeder
{
    private static readonly string[] Categories =
        ["Sneakers", "Boots", "Sandals", "Running"];

    private static readonly (string Name, decimal Price)[] Models =
    [
        ("Aurora", 89.90m), ("Bolt", 74.50m), ("Canyon", 119.00m),
        ("Drift", 64.90m), ("Ember", 99.00m), ("Flux", 129.90m),
    ];

    public static async Task SeedAsync(CatalogDbContext dbContext, TimeProvider clock,
        CancellationToken cancellationToken = default)
    {
        if (await dbContext.Categories.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = clock.GetUtcNow();
        foreach (var (categoryName, c) in Categories.Select((n, i) => (n, i)))
        {
            var category = new Category { Id = Guid.CreateVersion7(), Name = categoryName };
            dbContext.Categories.Add(category);

            foreach (var ((modelName, price), m) in Models.Select((x, i) => (x, i)))
            {
                dbContext.Products.Add(new Product
                {
                    Id = Guid.CreateVersion7(),
                    Sku = $"SHP-{c + 1}{m + 1:D2}",
                    Name = $"{modelName} {categoryName[..^1]}",
                    Price = price,
                    CategoryId = category.Id,
                    CreatedAtUtc = now,
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
