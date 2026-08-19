using Microsoft.EntityFrameworkCore.Design;

namespace Sphere.Catalog.Data;

/// <summary>Used only by the `dotnet ef` command-line tool.</summary>
internal sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=sphere;Username=sphere;Password=sphere-dev")
            .Options;

        return new CatalogDbContext(options);
    }
}
