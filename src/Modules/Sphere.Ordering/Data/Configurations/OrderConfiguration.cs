using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Sphere.Ordering.Data.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id).HasColumnName("id");
        builder.Property(o => o.CustomerId).HasColumnName("customer_id");
        // why: the enumeration persists as its integer id — one small conversion.
        builder.Property(o => o.Status).HasColumnName("status_id")
            .HasConversion(s => s.Id, id => OrderStatus.FromId(id));
        builder.Property(o => o.Currency).HasColumnName("currency").HasMaxLength(3);
        builder.Property(o => o.Total).HasColumnName("total").HasColumnType("numeric(12,2)");
        builder.Property(o => o.PlacedAtUtc).HasColumnName("placed_at_utc");

        builder.HasIndex(o => o.CustomerId).HasDatabaseName("ix_orders_customer_id");

        // why: the collection maps through the backing FIELD, so nobody can Add to the
        // public list around the aggregate's rules.
        builder.HasMany(o => o.Lines).WithOne()
            .HasForeignKey("order_id").HasConstraintName("fk_order_lines_order");
        builder.Navigation(o => o.Lines)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasField("_lines");
    }
}
