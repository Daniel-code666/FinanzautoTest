using Finanzauto.Domain.Entities;
using Finanzauto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace Finanzauto.IntegrationTests;

[Collection("PostgreSQL")]
public class CustomerMigrationTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Migration_preserves_existing_customers_orders_audit_and_cascade()
    {
        var database = "migration_" + Guid.NewGuid().ToString("N");
        await using var connection = new NpgsqlConnection(postgres.Container.GetConnectionString());
        await connection.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE {database}", connection))
            await create.ExecuteNonQueryAsync();
        try
        {
            var connectionString = new NpgsqlConnectionStringBuilder(postgres.Container.GetConnectionString()) { Database = database }.ConnectionString;
            await using var db = new FinanzautoDbContext(new DbContextOptionsBuilder<FinanzautoDbContext>().UseNpgsql(connectionString).Options);
            await db.GetService<IMigrator>().MigrateAsync("20260923222434_AuditDatesWithoutSecurityStamp");
            var employee = new Employee { FirstName = "Migration", LastName = "Test", Email = "migration@example.test", PasswordHash = "unused", RoleId = 1 };
            db.Employees.Add(employee);
            await db.SaveChangesAsync();
            await db.Database.ExecuteSqlRawAsync("""
                INSERT INTO "Customers" ("CustomerId", "CompanyName", "Active")
                VALUES ('AB01', 'Existing active', true), ('ZZ99', 'Existing inactive', false);
                UPDATE "Customers" SET "City" = 'Bogota' WHERE "CustomerId" = 'AB01';
                """);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "Orders" ("CustomerId", "EmployeeId", "OrderDate", "Freight")
                VALUES ('AB01', {employee.EmployeeId}, CURRENT_TIMESTAMP, 0),
                       ('ZZ99', {employee.EmployeeId}, CURRENT_TIMESTAMP, 0);
                """);
            // Copias temporales permiten comparar las fechas exactas antes y despu?s.
            await db.Database.OpenConnectionAsync();
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TEMP TABLE "CustomerAuditBefore" AS
                SELECT "CompanyName", "CreationDate", "UpdatedDate" FROM "Customers";
                CREATE TEMP TABLE "OrderAuditBefore" AS
                SELECT "OrderId", "CreationDate", "UpdatedDate" FROM "Orders";
                """);
            await db.Database.MigrateAsync();
            db.ChangeTracker.Clear();
            var customers = await db.Customers.IgnoreQueryFilters().OrderBy(c => c.CustomerId).ToListAsync();
            Assert.Equal(2, customers.Count);
            Assert.All(customers, c => Assert.True(c.CustomerId > 0));
            var active = customers.Single(c => c.Active);
            var inactive = customers.Single(c => !c.Active);
            var orders = await db.Orders.IgnoreQueryFilters().OrderBy(o => o.OrderId).ToListAsync();
            Assert.Equal(active.CustomerId, orders[0].CustomerId);
            Assert.Equal(inactive.CustomerId, orders[1].CustomerId);
            var changedDates = await db.Database.SqlQueryRaw<int>("""
                SELECT CAST(count(*) AS integer) AS "Value" FROM (
                    SELECT c."CreationDate", c."UpdatedDate", b."CreationDate" AS old_created, b."UpdatedDate" AS old_updated
                    FROM "Customers" c JOIN "CustomerAuditBefore" b USING ("CompanyName")
                    UNION ALL
                    SELECT o."CreationDate", o."UpdatedDate", b."CreationDate", b."UpdatedDate"
                    FROM "Orders" o JOIN "OrderAuditBefore" b USING ("OrderId")
                ) dates WHERE "CreationDate" IS DISTINCT FROM old_created OR "UpdatedDate" IS DISTINCT FROM old_updated
                """).SingleAsync();
            Assert.Equal(0, changedDates);
            var next = new Customer { CompanyName = "New customer" };
            db.Customers.Add(next);
            await db.SaveChangesAsync();
            Assert.True(next.CustomerId > customers.Max(c => c.CustomerId));
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"Customers\" WHERE \"CustomerId\" = {active.CustomerId}");
            Assert.False(await db.Orders.IgnoreQueryFilters().AnyAsync(o => o.CustomerId == active.CustomerId));
            Assert.True(await db.Orders.IgnoreQueryFilters().AnyAsync(o => o.CustomerId == inactive.CustomerId));
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Customers\" SET \"City\" = 'Changed' WHERE \"CustomerId\" = {next.CustomerId}");
            await db.Entry(next).ReloadAsync();
            Assert.NotNull(next.UpdatedDate);
        }
        finally
        {
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS {database} WITH (FORCE)", connection);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
