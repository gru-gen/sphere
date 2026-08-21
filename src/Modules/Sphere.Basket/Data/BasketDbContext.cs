namespace Sphere.Basket.Data;

internal sealed class BasketDbContext(DbContextOptions<BasketDbContext> options)
    : DbContext(options)
{
    public DbSet<BasketItem> Items => Set<BasketItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("basket");

        modelBuilder.Entity<BasketItem>(b =>
        {
            b.ToTable("basket_items");

            b.HasKey(i => new { i.CustomerId, i.ProductId }).HasName("pk_basket_items");

            b.Property(i => i.CustomerId).HasColumnName("customer_id");
            b.Property(i => i.ProductId).HasColumnName("product_id");
            b.Property(i => i.Quantity).HasColumnName("quantity");
            b.Property(i => i.UpdatedAtUtc).HasColumnName("updated_at_utc");
        });
    }
}
