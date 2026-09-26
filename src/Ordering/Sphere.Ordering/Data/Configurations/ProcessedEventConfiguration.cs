using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Sphere.Ordering.Data.Configurations;

internal sealed class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
    {
        builder.ToTable("processed_events");

        builder.HasKey(e => e.EventId);

        builder.Property(e => e.EventId).HasColumnName("event_id");
        builder.Property(e => e.ProcessedAtUtc).HasColumnName("processed_at_utc");
    }
}
