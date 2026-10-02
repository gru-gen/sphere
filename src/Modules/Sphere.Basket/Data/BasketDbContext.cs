namespace Sphere.Basket.Data;

internal sealed class BasketDbContext(DbContextOptions<BasketDbContext> options)
    : DbContext(options)
{
    public DbSet<BasketItem> Items => Set<BasketItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("basket");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BasketDbContext).Assembly);
    }
}
