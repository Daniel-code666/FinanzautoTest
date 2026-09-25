using Finanzauto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finanzauto.Infrastructure.Persistence.Configurations;

public sealed class OrderDetailConfiguration : IEntityTypeConfiguration<OrderDetail>
{
    public void Configure(EntityTypeBuilder<OrderDetail> b)
    {
        b.ToTable("OrderDetails");
        b.Property(x => x.Active).HasDefaultValue(true);
        b.HasQueryFilter(x => x.Active);
        b.HasKey(x => x.OrderDetailId);
        b.Property(x => x.OrderDetailId).UseIdentityByDefaultColumn();
        b.HasIndex(x => new { x.OrderId, x.ProductId }).IsUnique().HasFilter("\"Active\" = true");
        b.Property(x => x.UnitPrice).HasPrecision(18, 2);
        b.Property(x => x.Discount).HasPrecision(5, 4);
        b.HasOne(x => x.Order).WithMany(x => x.OrderDetails).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Product).WithMany(x => x.OrderDetails).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_OrderDetails_UnitPrice", "\"UnitPrice\" >= 0");
            t.HasCheckConstraint("CK_OrderDetails_Quantity", "\"Quantity\" > 0");
            t.HasCheckConstraint("CK_OrderDetails_Discount", "\"Discount\" >= 0 AND \"Discount\" <= 1");
        });
    }
}
