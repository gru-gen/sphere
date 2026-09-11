using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Sphere.Ordering.Data.Configurations;

internal sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_keys");

        builder.HasKey(r => r.Key);

        builder.Property(r => r.Key).HasColumnName("key").HasMaxLength(200);
        builder.Property(r => r.OrderId).HasColumnName("order_id");
        builder.Property(r => r.Total).HasColumnName("total").HasColumnType("numeric(12,2)");
        builder.Property(r => r.Currency).HasColumnName("currency").HasMaxLength(3);
        builder.Property(r => r.CreatedAtUtc).HasColumnName("created_at_utc");
    }
}
