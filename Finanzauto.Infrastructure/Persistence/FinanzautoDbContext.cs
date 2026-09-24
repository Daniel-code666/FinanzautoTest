using Finanzauto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Finanzauto.Infrastructure.Persistence;

public sealed class FinanzautoDbContext : DbContext
{
    public FinanzautoDbContext(DbContextOptions<FinanzautoDbContext> options) : base(options)
    {
        // Las cascadas físicas pertenecen a PostgreSQL. EF convierte Remove en desactivación.
        ChangeTracker.CascadeDeleteTiming = CascadeTiming.OnSaveChanges;
        ChangeTracker.DeleteOrphansTiming = CascadeTiming.OnSaveChanges;
    }

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
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(type => typeof(AuditTable).IsAssignableFrom(type.ClrType)))
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

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        // Los detalles deben cargarse antes de convertir el borrado del pedido en un UPDATE.
        var orderIds = GetDeactivatedOrderIds();
        if (orderIds.Length > 0)
        {
            OrderDetails.IgnoreQueryFilters().Where(x => orderIds.Contains(x.OrderId) && x.Active).Load();
            DeactivateOrderDetails(orderIds);
        }
        PrepareChanges();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var orderIds = GetDeactivatedOrderIds();
        if (orderIds.Length > 0)
        {
            await OrderDetails.IgnoreQueryFilters()
                .Where(x => orderIds.Contains(x.OrderId) && x.Active).LoadAsync(cancellationToken);
            DeactivateOrderDetails(orderIds);
        }
        PrepareChanges();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private int[] GetDeactivatedOrderIds()
    {
        return ChangeTracker.Entries<Order>()
            .Where(x => x.State == EntityState.Deleted ||
                (x.State == EntityState.Modified && !x.Entity.Active))
            .Select(x => x.Entity.OrderId)
            .ToArray();
    }

    private void DeactivateOrderDetails(int[] orderIds)
    {
        foreach (var entry in ChangeTracker.Entries<OrderDetail>()
                     .Where(x => orderIds.Contains(x.Entity.OrderId)))
        {
            entry.Entity.Active = false;
        }
    }

    private void PrepareChanges()
    {
        ConvertDeletesToSoftDeletes();
        NormalizeEmployeeEmails();
    }

    private void ConvertDeletesToSoftDeletes()
    {
        foreach (var entry in ChangeTracker.Entries<Entity>()
                     .Where(x => x.State == EntityState.Deleted))
        {
            // Se descartan otros cambios pendientes de esta entidad y solo se guarda Active=false.
            entry.State = EntityState.Unchanged;
            entry.Entity.Active = false;
            entry.Property(x => x.Active).IsModified = true;
        }
    }

    private void NormalizeEmployeeEmails()
    {
        foreach (var entry in ChangeTracker.Entries<Employee>()
                     .Where(x => x.State == EntityState.Added || x.State == EntityState.Modified))
        {
            entry.Entity.Email = entry.Entity.Email.Trim();
            entry.Entity.NormalizedEmail = entry.Entity.Email.ToUpperInvariant();
        }
    }
}
