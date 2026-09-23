using Finanzauto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finanzauto.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("Roles");
        b.Property(x => x.Active).HasDefaultValue(true);
        b.HasQueryFilter(x => x.Active);
        b.HasKey(x => x.RoleId);
        b.Property(x => x.Name).HasMaxLength(50).IsRequired();
        b.HasIndex(x => x.Name).IsUnique();
        b.Property(x => x.NormalizedName).HasMaxLength(50)
            .HasComputedColumnSql("upper(btrim(\"Name\"))", stored: true);
        b.HasIndex(x => x.NormalizedName).IsUnique();
        b.HasData(
            new Role { RoleId = 1, Name = "Admin" },
            new Role { RoleId = 2, Name = "User" });
    }
}
