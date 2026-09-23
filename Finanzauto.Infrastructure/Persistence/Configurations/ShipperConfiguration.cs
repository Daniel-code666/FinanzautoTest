using Finanzauto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finanzauto.Infrastructure.Persistence.Configurations;

public sealed class ShipperConfiguration : IEntityTypeConfiguration<Shipper>
{
    public void Configure(EntityTypeBuilder<Shipper> b)
    {
        b.ToTable("Shippers");
        b.Property(x => x.Active).HasDefaultValue(true);
        b.HasQueryFilter(x => x.Active);
        b.HasKey(x => x.ShipperId);
        b.Property(x => x.CompanyName).HasMaxLength(200).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(30);
    }
}

