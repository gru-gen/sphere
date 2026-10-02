using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Sphere.Ordering.Data.Configurations;

internal sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("order_lines");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id).HasColumnName("id");
        builder.Property<Guid>("order_id");
        builder.Property(l => l.ProductId).HasColumnName("product_id");
        builder.Property(l => l.ProductName).HasColumnName("product_name").HasMaxLength(200);
        builder.Property(l => l.UnitPrice).HasColumnName("unit_price").HasColumnType("numeric(12,2)");
        builder.Property(l => l.Quantity).HasColumnName("quantity");

        builder.HasIndex("order_id").HasDatabaseName("ix_order_lines_order_id");
    }
}
