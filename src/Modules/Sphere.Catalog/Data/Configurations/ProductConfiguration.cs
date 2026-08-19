using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Sphere.Catalog.Data.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.Sku).HasColumnName("sku").HasMaxLength(32);
        builder.Property(p => p.Name).HasColumnName("name").HasMaxLength(200);
        builder.Property(p => p.Price).HasColumnName("price").HasColumnType("numeric(12,2)");
        builder.Property(p => p.CategoryId).HasColumnName("category_id");
        builder.Property(p => p.CreatedAtUtc).HasColumnName("created_at_utc");

        builder.HasIndex(p => p.Sku).IsUnique().HasDatabaseName("ux_products_sku");
        builder.HasIndex(p => p.CategoryId).HasDatabaseName("ix_products_category_id");

        builder.HasOne<Category>().WithMany()
            .HasForeignKey(p => p.CategoryId)
            .HasConstraintName("fk_products_category");
    }
}
