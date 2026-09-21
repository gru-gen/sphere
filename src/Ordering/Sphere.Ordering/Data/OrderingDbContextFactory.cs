using Microsoft.EntityFrameworkCore.Design;

namespace Sphere.Ordering.Data;

/// <summary>Used only by the `dotnet ef` command-line tool.</summary>
internal sealed class OrderingDbContextFactory : IDesignTimeDbContextFactory<OrderingDbContext>
{
    public OrderingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OrderingDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=sphere;Username=sphere;Password=sphere-dev")
            .Options;

        return new OrderingDbContext(options, null!);
    }
}
