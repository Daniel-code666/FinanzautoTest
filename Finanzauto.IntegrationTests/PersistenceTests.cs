using System.Net;
using System.Net.Http.Json;
using Finanzauto.Application.Catalog;
using Finanzauto.Domain.Entities;
using Finanzauto.Infrastructure.Catalog;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Finanzauto.IntegrationTests;

[Collection("PostgreSQL")]
public sealed class PersistenceTests(PostgresFixture postgres) : ApiTest(postgres)
{
    [Fact]
    public async Task COPY_failure_rolls_back_even_preceding_valid_rows()
    {
        await using var db = Db();
        var category = new Category { CategoryName = "Rollback" };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        var supplier = await db.Suppliers.FirstAsync();
        Product Make(string name, decimal price) => new()
        {
            ProductName = name,
            CategoryId = category.CategoryId,
            SupplierId = supplier.SupplierId,
            UnitPrice = price,
            QuantityPerUnit = "1"
        };
        var writer = new BulkProductWriter(db);
        var error = await Assert.ThrowsAsync<PostgresException>(() => writer.WriteAsync(
            [Make("Valid row", 10), Make("Invalid row", -1)], [category.CategoryId], supplier.SupplierId, default));
        Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
        await using var verification = Db();
        Assert.Equal(0, await verification.Products.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task Category_update_delete_and_reference_protection()
    {
        var response = await Admin.PostAsJsonAsync("/Category", new { categoryName = "Original" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var category = (await response.Content.ReadFromJsonAsync<CategoryDetailResponse>())!;
        Assert.Equal(HttpStatusCode.OK, (await Admin.PutAsJsonAsync($"/Categories/{category.Id}",
            new { categoryName = "Renamed", description = "Updated" })).StatusCode);
        var page = (await Admin.GetFromJsonAsync<CatalogPage<CategoryResponse>>("/Categories?search=renamed&pageSize=1"))!;
        Assert.Equal(category.Id, Assert.Single(page.Items).Id);
        await using var db = Db();
        var supplier = await db.Suppliers.FirstAsync();
        var product = new Product { ProductName = "Dependent", CategoryId = category.Id, SupplierId = supplier.SupplierId };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Conflict, (await Admin.DeleteAsync($"/Categories/{category.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Admin.DeleteAsync($"/Products/{product.ProductId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Admin.DeleteAsync($"/Categories/{category.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.GetAsync($"/Categories/{category.Id}")).StatusCode);
        Assert.False((await db.Categories.IgnoreQueryFilters().AsNoTracking().SingleAsync(c => c.CategoryId == category.Id)).Active);
    }

    [Fact]
    public async Task Direct_SQL_updates_audit_and_physical_delete_cascades()
    {
        await using var db = Db();
        var category = new Category { CategoryName = "Audit" };
        var supplier = await db.Suppliers.FirstAsync();
        var product = new Product { ProductName = "Before", Category = category, SupplierId = supplier.SupplierId };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        var created = product.CreationDate;
        Assert.NotEqual(default, created);
        Assert.Null(product.UpdatedDate);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"Products\" SET \"ProductName\" = 'After' WHERE \"ProductId\" = {product.ProductId}");
        await db.Entry(product).ReloadAsync();
        Assert.Equal(created, product.CreationDate);
        Assert.NotNull(product.UpdatedDate);
        var updated = product.UpdatedDate;
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"Products\" SET \"ProductName\" = 'After' WHERE \"ProductId\" = {product.ProductId}");
        await db.Entry(product).ReloadAsync();
        Assert.Equal(updated, product.UpdatedDate);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM \"Categories\" WHERE \"CategoryId\" = {category.CategoryId}");
        Assert.False(await db.Products.IgnoreQueryFilters().AnyAsync(p => p.ProductId == product.ProductId));
    }
}
