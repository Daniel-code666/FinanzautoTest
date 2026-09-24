using Finanzauto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finanzauto.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.ToTable("Products");
        b.Property(x => x.Active).HasDefaultValue(true);
        b.HasQueryFilter(x => x.Active);
        b.HasKey(x => x.ProductId);
        b.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
        b.Property(x => x.QuantityPerUnit).HasMaxLength(100);
        b.Property(x => x.UnitPrice).HasPrecision(18, 2);
        b.HasIndex(x => new { x.Active, x.CategoryId, x.ProductId });
        b.HasIndex(x => x.ProductName);
        b.Property<string>("SearchName").HasMaxLength(200)
            .HasComputedColumnSql("upper(\"ProductName\")", stored: true);
        b.HasIndex("SearchName").HasMethod("gin").HasOperators("gin_trgm_ops");
        b.HasIndex(x => new { x.Active, x.UnitPrice, x.ProductId });
        b.HasOne(x => x.Category).WithMany(x => x.Products).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Supplier).WithMany(x => x.Products).HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Cascade);
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Products_UnitPrice", "\"UnitPrice\" >= 0");
            t.HasCheckConstraint("CK_Products_Stock", "\"UnitsInStock\" >= 0 AND \"UnitsOnOrder\" >= 0 AND \"ReorderLevel\" >= 0");
        });
    }
}
