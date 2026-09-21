using Microsoft.EntityFrameworkCore.Design;

namespace Sphere.Basket.Data;

/// <summary>Used only by the `dotnet ef` command-line tool.</summary>
internal sealed class BasketDbContextFactory : IDesignTimeDbContextFactory<BasketDbContext>
{
    public BasketDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BasketDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=sphere;Username=sphere;Password=sphere-dev")
            .Options;

        return new BasketDbContext(options);
    }
}
