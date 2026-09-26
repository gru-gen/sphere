namespace Sphere.Basket.Data;

internal sealed class BasketDbContext(DbContextOptions<BasketDbContext> options)
    : DbContext(options)
{
    public DbSet<BasketItem> Items => Set<BasketItem>();
    public DbSet<CheckoutRecord> Checkouts => Set<CheckoutRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("basket");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BasketDbContext).Assembly);
    }
}
