using Finanzauto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finanzauto.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.ToTable("Categories");
        b.Property(x => x.Active).HasDefaultValue(true);
        b.HasQueryFilter(x => x.Active);
        b.HasKey(x => x.CategoryId);
        b.Property(x => x.CategoryName).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.CategoryName).IsUnique();
        b.Property<string>("NormalizedName").HasMaxLength(100)
            .HasComputedColumnSql("upper(btrim(\"CategoryName\"))", stored: true);
        b.HasIndex("NormalizedName").IsUnique();
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.PictureContentType).HasMaxLength(100);
    }
}
