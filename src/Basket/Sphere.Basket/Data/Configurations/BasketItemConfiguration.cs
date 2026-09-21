using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Sphere.Basket.Data.Configurations;

internal sealed class BasketItemConfiguration : IEntityTypeConfiguration<BasketItem>
{
    public void Configure(EntityTypeBuilder<BasketItem> builder)
    {
        builder.ToTable("basket_items");

        // why: the pair is the identity — one product appears once per customer.
        builder.HasKey(i => new { i.CustomerId, i.ProductId }).HasName("pk_basket_items");

        builder.Property(i => i.CustomerId).HasColumnName("customer_id");
        builder.Property(i => i.ProductId).HasColumnName("product_id");
        builder.Property(i => i.Quantity).HasColumnName("quantity");
        builder.Property(i => i.UpdatedAtUtc).HasColumnName("updated_at_utc");
    }
}
