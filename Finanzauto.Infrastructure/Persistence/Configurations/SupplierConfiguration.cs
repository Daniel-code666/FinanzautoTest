using Finanzauto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finanzauto.Infrastructure.Persistence.Configurations;

public sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> b)
    {
        b.ToTable("Suppliers");
        b.Property(x => x.Active).HasDefaultValue(true);
        b.HasQueryFilter(x => x.Active);
        b.HasKey(x => x.SupplierId);
        b.HasData(new Supplier { SupplierId = 1, CompanyName = "Proveedor inicial" });
        b.Property(x => x.CompanyName).HasMaxLength(200).IsRequired();
        b.HasIndex(x => x.CompanyName);
        b.Property(x => x.HomePage).HasMaxLength(2048);
        b.Property(x => x.Address).HasMaxLength(300);
        b.Property(x => x.City).HasMaxLength(100);
        b.Property(x => x.Region).HasMaxLength(100);
        b.Property(x => x.PostalCode).HasMaxLength(20);
        b.Property(x => x.Country).HasMaxLength(100);
        b.Property(x => x.ContactName).HasMaxLength(100);
        b.Property(x => x.ContactTitle).HasMaxLength(100);
        b.Property(x => x.Phone).HasMaxLength(30);
        b.Property(x => x.Fax).HasMaxLength(30);
    }
}
