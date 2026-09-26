using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Sphere.Basket.Data.Configurations;

internal sealed class CheckoutRecordConfiguration : IEntityTypeConfiguration<CheckoutRecord>
{
    public void Configure(EntityTypeBuilder<CheckoutRecord> builder)
    {
        builder.ToTable("checkouts");

        builder.HasKey(c => c.Key).HasName("pk_checkouts");

        builder.Property(c => c.Key).HasColumnName("key").HasMaxLength(200);
        builder.Property(c => c.OrderId).HasColumnName("order_id");
        builder.Property(c => c.CreatedAtUtc).HasColumnName("created_at_utc");
    }
}
