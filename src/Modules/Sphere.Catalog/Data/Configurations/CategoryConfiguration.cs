using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Sphere.Catalog.Data.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(c => c.Id);

        builder.Property(с => с.Id).HasColumnName("id");
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(100);

        builder.HasIndex(c => c.Name).IsUnique().HasDatabaseName("ux_categories_name");
    }
}
