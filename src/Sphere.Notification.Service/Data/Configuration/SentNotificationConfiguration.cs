using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Sphere.Notification.Service.Data.Configuration;

internal sealed class SentNotificationConfiguration : IEntityTypeConfiguration<SentNotification>
{
    public void Configure(EntityTypeBuilder<SentNotification> builder)
    {
        builder.ToTable("sent_notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id).HasColumnName("id");
        builder.Property(n => n.OrderId).HasColumnName("order_id");
        builder.Property(n => n.CustomerId).HasColumnName("customer_id");
        builder.Property(n => n.Channel).HasColumnName("channel").HasMaxLength(10);
        builder.Property(n => n.Subject).HasColumnName("subject").HasMaxLength(200);
        builder.Property(n => n.Body).HasColumnName("body");
        builder.Property(n => n.CreatedAtUtc).HasColumnName("created_at_utc");

        builder.HasIndex(n => new { n.OrderId, n.Channel })
            .IsUnique().HasDatabaseName("ux_sent_notifications_order_channel");
    }
}
