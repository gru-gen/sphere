using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Sphere.Ordering.Data.Configurations;

internal sealed class OrderHistoryConfiguration : IEntityTypeConfiguration<OrderHistoryEntry>
{
    public void Configure(EntityTypeBuilder<OrderHistoryEntry> builder)
    {
        builder.ToTable("order_history");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id).HasColumnName("id");
        builder.Property(h => h.OrderId).HasColumnName("order_id");
        builder.Property(h => h.AtUtc).HasColumnName("at_utc");
        builder.Property(h => h.What).HasColumnName("what").HasMaxLength(200);

        builder.HasIndex(h => h.OrderId).HasDatabaseName("ix_order_history_order_id");
    }
}
