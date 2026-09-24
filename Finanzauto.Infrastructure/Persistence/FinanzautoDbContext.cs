using Finanzauto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Finanzauto.Infrastructure.Persistence;

public sealed class FinanzautoDbContext(DbContextOptions<FinanzautoDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Shipper> Shippers => Set<Shipper>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinanzautoDbContext).Assembly);
        ConfigureAuditDates(modelBuilder);
    }

    private static void ConfigureAuditDates(ModelBuilder modelBuilder)
    {
        // PostgreSQL asigna estas fechas con triggers, también para EF y SQL directo.
        // EF las lee al guardar y evita enviar valores escritos por la aplicación.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(type => typeof(AuditTable).IsAssignableFrom(type.ClrType)))
        {
            var entity = modelBuilder.Entity(entityType.ClrType);
            var creation = entity.Property<DateTime>(nameof(AuditTable.CreationDate))
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAdd();
            creation.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
            creation.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

            var updated = entity.Property<DateTime?>(nameof(AuditTable.UpdatedDate))
                .HasColumnType("timestamp with time zone").ValueGeneratedOnAddOrUpdate();
            updated.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
            updated.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        }
    }

}
