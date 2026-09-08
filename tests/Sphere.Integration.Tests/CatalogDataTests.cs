using Microsoft.EntityFrameworkCore;
using Npgsql;
using Sphere.Catalog.Data;
using Sphere.Catalog.Domain;
using Sphere.Catalog.Features.Products;

namespace Sphere.Integration.Tests;

[Collection("postgres")]
public class CatalogDataTests(PostgresFixture postgresFixture)
{
    [Fact]
    public async Task Browse_filters_by_category_and_pages_in_name_order()
    {
        await using var db = postgresFixture.CreateCatalogContext();
        var category = await SeedCategoryWithProducts(db, "Alpha", "Bravo", "Charlie");

        var page = await GetProducts.Handle(
            new GetProducts.Request(Page: 1, PageSize: 2, CategoryId: category),
            db, CancellationToken.None);

        Assert.Equal(3, page.Value!.Total);
        Assert.Equal(["Alpha", "Bravo"], page.Value.Items.Select(i => i.Name));
    }

    [Fact]
    public async Task The_unique_index_itself_rejects_a_duplicate_sku()
    {
        await using var db = postgresFixture.CreateCatalogContext();
        var category = await SeedCategoryWithProducts(db, "Single");
        var sku = (await db.Products.AsNoTracking()
            .FirstAsync(p => p.CategoryId == category)).Sku;

        // why: bypass the polite precheck on purpose — this is the RACE path,
        // where only the constraint can still say no.
        db.Products.Add(NewProduct(sku, "Impostor", category));
        var ex = await Assert.ThrowsAsync<DbUpdateException>(
            () => db.SaveChangesAsync());

        var pg = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, pg.SqlState);
    }

    [Fact]
    public async Task Keyset_scroll_crosses_name_ties_without_gaps_or_repeats()
    {
        await using var db = postgresFixture.CreateCatalogContext();
        // why: three identical names — the (name, id) tiebreaker earns its seat.
        await SeedCategoryWithProducts(db, "Tie", "Tie", "Tie", "Zed", "Zed");

        var seen = new List<Guid>();
        string? afterName = null;
        Guid? afterId = null;
        for (var i = 0; i < 5; i++)
        {
            var page = await ScrollProducts.Handle(
                new ScrollProducts.Request(afterName, afterId, PageSize: 2),
                db, CancellationToken.None);
            seen.AddRange(page.Value!.Items.Select(x => x.Id));
            (afterName, afterId) = (page.Value.NextAfterName, page.Value.NextAfterId);
            if (afterName is null) break;
        }

        Assert.Equal(seen.Count, seen.Distinct().Count());
        Assert.True(seen.Count >= 5);
    }

    private static async Task<Guid> SeedCategoryWithProducts(
        CatalogDbContext dbContext, params string[] names)
    {
        var category = new Category
        {
            Id = Guid.CreateVersion7(),
            Name = $"cat-{Guid.CreateVersion7():N}",
        };

        dbContext.Categories.Add(category);
        foreach (var name in names)
        {
            dbContext.Products.Add(NewProduct($"T-{Guid.CreateVersion7():N}"[..24], name, category.Id));
        }

        await dbContext.SaveChangesAsync();
        return category.Id;
    }

    private static Product NewProduct(string sku, string name, Guid categoryId) => new()
    {
        Id = Guid.CreateVersion7(),
        Sku = sku.ToUpperInvariant(),
        Name = name,
        Price = 10m,
        CategoryId = categoryId,
        CreatedAtUtc = DateTime.UtcNow,
    };
}
