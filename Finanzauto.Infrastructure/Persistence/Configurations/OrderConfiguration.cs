using Finanzauto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finanzauto.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.ToTable("Orders");
        b.Property(x => x.Active).HasDefaultValue(true);
        b.HasQueryFilter(x => x.Active);
        b.HasKey(x => x.OrderId);
        b.Property(x => x.CustomerId).HasMaxLength(5);
        b.Property(x => x.Freight).HasPrecision(18, 2);
        b.Property(x => x.ShipName).HasMaxLength(200);
        b.Property(x => x.ShipAddress).HasMaxLength(300);
        b.Property(x => x.ShipCity).HasMaxLength(100);
        b.Property(x => x.ShipRegion).HasMaxLength(100);
        b.Property(x => x.ShipPostalCode).HasMaxLength(20);
        b.Property(x => x.ShipCountry).HasMaxLength(100);
        b.HasOne(x => x.Customer).WithMany(x => x.Orders).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Employee).WithMany(x => x.Orders).HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Shipper).WithMany(x => x.Orders).HasForeignKey(x => x.ShipVia).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.Active, x.OrderDate, x.OrderId });
        b.ToTable(t => t.HasCheckConstraint("CK_Orders_Freight", "\"Freight\" >= 0"));
    }
}

