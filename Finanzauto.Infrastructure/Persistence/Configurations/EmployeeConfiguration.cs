using Finanzauto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finanzauto.Infrastructure.Persistence.Configurations;

public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> b)
    {
        b.ToTable("Employees");
        b.Property(x => x.Active).HasDefaultValue(true);
        b.HasQueryFilter(x => x.Active);
        b.HasKey(x => x.EmployeeId);
        b.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        b.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        b.Property(x => x.Title).HasMaxLength(100);
        b.Property(x => x.TitleOfCourtesy).HasMaxLength(30);
        b.Property(x => x.HomePhone).HasMaxLength(30);
        b.Property(x => x.Extension).HasMaxLength(10);
        b.Property(x => x.Email).HasMaxLength(254).IsRequired();
        b.Property(x => x.NormalizedEmail).HasMaxLength(254).IsRequired();
        b.Property(x => x.PasswordHash).HasMaxLength(1024).IsRequired();
        b.HasIndex(x => x.NormalizedEmail).IsUnique();
        b.HasOne(x => x.Role).WithMany(x => x.Employees).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Supervisor).WithMany(x => x.Subordinates).HasForeignKey(x => x.ReportsTo).OnDelete(DeleteBehavior.Cascade);
        b.ToTable(t => t.HasCheckConstraint("CK_Employees_Supervisor", "\"ReportsTo\" IS NULL OR \"ReportsTo\" <> \"EmployeeId\""));
        b.Property(x => x.Address).HasMaxLength(300);
        b.Property(x => x.City).HasMaxLength(100);
        b.Property(x => x.Region).HasMaxLength(100);
        b.Property(x => x.PostalCode).HasMaxLength(20);
        b.Property(x => x.Country).HasMaxLength(100);
    }
}
