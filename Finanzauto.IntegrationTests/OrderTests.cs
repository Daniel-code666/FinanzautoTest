using System.Net;
using System.Net.Http.Json;
using Finanzauto.Application.Catalog;
using Finanzauto.Application.Orders;
using Finanzauto.Domain.Entities;
using Finanzauto.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace Finanzauto.IntegrationTests;

[Collection("PostgreSQL")]
public sealed class OrderTests(PostgresFixture postgres) : ApiTest(postgres)
{
    private async Task<(int Customer, int Shipper, int[] Products)> References()
    {
        await using var db = Db();
        var customer = new Customer { CompanyName = "Orders customer" };
        var shipper = new Shipper { CompanyName = "Orders shipper" };
        var category = new Category { CategoryName = "Orders category" };
        var supplier = await db.Suppliers.FirstAsync();
        var products = Enumerable.Range(1, 3).Select(i => new Product
        {
            ProductName = $"Order product {i}",
            Category = category,
            SupplierId = supplier.SupplierId,
            UnitPrice = 10 * i,
            UnitsInStock = 100
        }).ToArray();
        db.Customers.Add(customer);
        db.Shippers.Add(shipper);
        db.Products.AddRange(products);
        await db.SaveChangesAsync();
        return (customer.CustomerId, shipper.ShipperId, products.Select(x => x.ProductId).ToArray());
    }

    private static OrderRequest Request(int customer, int shipper, params int[] products) => new()
    {
        CustomerId = customer,
        ShipVia = shipper,
        OrderDate = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc),
        Freight = 5,
        ShipName = " Office ",
        ShipCity = " Bogota ",
        Details = products.Select(id => new OrderDetailRequest { ProductId = id, Quantity = 2, Discount = 0.1m }).ToArray()
    };

    private static async Task<OrderResponse> Read(HttpResponseMessage response)
    {
        using (response)
        {
            Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromJsonAsync<OrderResponse>())!;
        }
    }

    [Fact]
    public async Task Create_update_add_remove_readd_and_deactivate_preserve_header_and_history()
    {
        var refs = await References();
        var (client, user) = await Register();
        using (client)
        {
            var request = Request(refs.Customer, refs.Shipper, refs.Products[0], refs.Products[1]);
            var order = await Read(await client.PostAsJsonAsync("/Orders", request));
            Assert.Equal(user.Id, order.EmployeeId);
            Assert.Equal(59m, order.Total);
            Assert.Equal("Office", order.ShipName);
            Assert.All(order.Details, d => Assert.True(d.OrderDetailId > 0));
            var first = order.Details[0];
            var second = order.Details[1];
            await using (var db = Db())
            {
                var product = await db.Products.FindAsync(refs.Products[0]);
                product!.UnitPrice = 99;
                await db.SaveChangesAsync();
            }
            request.ShipName = "Updated";
            request.Details = [
                new() { OrderDetailId = first.OrderDetailId, ProductId = first.ProductId, Quantity = 3, Discount = 0 },
                new() { ProductId = refs.Products[2], Quantity = 1 }];
            var updated = await Read(await Admin.PutAsJsonAsync($"/Orders/{order.Id}", request));
            Assert.Equal(user.Id, updated.EmployeeId);
            Assert.Equal(3, updated.Details.Count);
            Assert.Equal(10, updated.Details.Single(x => x.OrderDetailId == first.OrderDetailId).UnitPrice);
            Assert.Equal(2, updated.Details.Single(x => x.OrderDetailId == second.OrderDetailId).Quantity);
            Assert.Equal(order.CreationDate, updated.CreationDate);
            Assert.NotNull(updated.UpdatedDate);
            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/Orders/{order.Id}/Details/{first.OrderDetailId}")).StatusCode);
            var afterDelete = await Read(await client.GetAsync($"/Orders/{order.Id}"));
            Assert.Equal(2, afterDelete.Details.Count);
            request.Details = [new() { ProductId = first.ProductId, Quantity = 1 }];
            var readded = await Read(await client.PutAsJsonAsync($"/Orders/{order.Id}", request));
            var newDetail = readded.Details.Single(x => x.ProductId == first.ProductId);
            Assert.NotEqual(first.OrderDetailId, newDetail.OrderDetailId);
            Assert.Equal(99, newDetail.UnitPrice);
            request.Details = [new() { OrderDetailId = first.OrderDetailId, ProductId = first.ProductId, Quantity = 1 }];
            Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/Orders/{order.Id}", request)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/Orders/{order.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/Orders/{order.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/Orders/{order.Id}", request)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/Orders/{order.Id}/Details/{newDetail.OrderDetailId}")).StatusCode);
            Assert.Empty((await client.GetFromJsonAsync<CatalogPage<OrderResponse>>("/Orders"))!.Items);
            await using var verification = Db();
            Assert.False((await verification.Orders.IgnoreQueryFilters().SingleAsync(x => x.OrderId == order.Id)).Active);
            var history = await verification.OrderDetails.IgnoreQueryFilters().Where(x => x.OrderId == order.Id).ToListAsync();
            Assert.Equal(4, history.Count);
            Assert.All(history, d => Assert.False(d.Active));
            Assert.All(await verification.Products.ToListAsync(), p => Assert.Equal(100, p.UnitsInStock));
        }
    }

    [Fact]
    public async Task Invalid_details_and_duplicates_do_not_save_header_or_partial_changes()
    {
        var refs = await References();
        var request = Request(refs.Customer, refs.Shipper, refs.Products[0]);
        var order = await Read(await Admin.PostAsJsonAsync("/Orders", request));
        var other = await Read(await Admin.PostAsJsonAsync("/Orders", request));
        request.ShipName = "Must not persist";
        request.Details = [new() { OrderDetailId = other.Details[0].OrderDetailId, ProductId = refs.Products[0], Quantity = 9 }];
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.PutAsJsonAsync($"/Orders/{order.Id}", request)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.DeleteAsync($"/Orders/{order.Id}/Details/{other.Details[0].OrderDetailId}")).StatusCode);
        request.Details = [new() { ProductId = refs.Products[0], Quantity = 1 }];
        using var duplicate = await Admin.PutAsJsonAsync($"/Orders/{order.Id}", request);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(409, (await duplicate.Content.ReadFromJsonAsync<ApiErrorResponse>())!.HttpCode);
        request.Details = [
            new() { OrderDetailId = order.Details[0].OrderDetailId, ProductId = refs.Products[0], Quantity = 9 },
            new() { ProductId = int.MaxValue, Quantity = 1 }];
        Assert.Equal(HttpStatusCode.Conflict, (await Admin.PutAsJsonAsync($"/Orders/{order.Id}", request)).StatusCode);
        var unchanged = await Read(await Admin.GetAsync($"/Orders/{order.Id}"));
        Assert.Equal("Office", unchanged.ShipName);
        Assert.Equal(2, Assert.Single(unchanged.Details).Quantity);
        request.Details = [new() { ProductId = refs.Products[1], Quantity = 1 }, new() { ProductId = refs.Products[1], Quantity = 2 }];
        Assert.Equal(HttpStatusCode.Conflict, (await Admin.PostAsJsonAsync("/Orders", request)).StatusCode);
        await using var db = Db();
        Assert.Equal(2, await db.Orders.CountAsync());
    }

    [Fact]
    public async Task List_filters_pages_and_only_active_details_last_detail_does_not_delete_order()
    {
        var refs = await References();
        var request = Request(refs.Customer, refs.Shipper, refs.Products[0]);
        var first = await Read(await Admin.PostAsJsonAsync("/Orders", request));
        var second = await Read(await Admin.PostAsJsonAsync("/Orders", request));
        request.ShipCity = "Medellin";
        await Read(await Admin.PostAsJsonAsync("/Orders", request));
        var url = $"/Orders?search=%20bOgOtA%20&customerId={refs.Customer}&shipVia={refs.Shipper}&employeeId={first.EmployeeId}&fromDate=2026-09-01T00:00:00Z&toDate=2026-09-02T00:00:00Z&pageSize=1";
        var page = (await Admin.GetFromJsonAsync<CatalogPage<OrderResponse>>(url + "&page=2"))!;
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(second.Id, Assert.Single(page.Items).Id);
        Assert.Single(page.Items[0].Details);
        Assert.Empty((await Admin.GetFromJsonAsync<CatalogPage<OrderResponse>>(url + "&page=3"))!.Items);
        Assert.Equal(HttpStatusCode.NoContent, (await Admin.DeleteAsync($"/Orders/{first.Id}/Details/{first.Details[0].OrderDetailId}")).StatusCode);
        Assert.Empty((await Read(await Admin.GetAsync($"/Orders/{first.Id}"))).Details);
        Assert.Equal(3, (await Admin.GetFromJsonAsync<CatalogPage<OrderResponse>>("/Orders"))!.TotalCount);
    }

    [Fact]
    public async Task Validation_and_authentication_reject_invalid_requests()
    {
        var refs = await References();
        var request = Request(refs.Customer, refs.Shipper, refs.Products[0]);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Anonymous.GetAsync("/Orders")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Anonymous.GetAsync("/Orders/1")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Anonymous.PostAsJsonAsync("/Orders", request)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Anonymous.PutAsJsonAsync("/Orders/1", request)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Anonymous.DeleteAsync("/Orders/1")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Anonymous.DeleteAsync("/Orders/1/Details/1")).StatusCode);
        request.Details[0].Quantity = 0;
        Assert.Equal(HttpStatusCode.BadRequest, (await Admin.PostAsJsonAsync("/Orders", request)).StatusCode);
        request.Details = [];
        Assert.Equal(HttpStatusCode.BadRequest, (await Admin.PostAsJsonAsync("/Orders", request)).StatusCode);
        request.Details = [null!];
        Assert.Equal(HttpStatusCode.BadRequest, (await Admin.PostAsJsonAsync("/Orders", request)).StatusCode);
        request.Details = [new() { ProductId = refs.Products[0], Quantity = 1, Discount = 1.1m }];
        Assert.Equal(HttpStatusCode.BadRequest, (await Admin.PostAsJsonAsync("/Orders", request)).StatusCode);
        request.Details[0].Discount = 0;
        request.CustomerId = int.MaxValue;
        Assert.Equal(HttpStatusCode.Conflict, (await Admin.PostAsJsonAsync("/Orders", request)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Admin.GetAsync("/Orders?pageSize=101")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Admin.GetAsync("/Orders?fromDate=2026-09-02T00:00:00Z&toDate=2026-09-01T00:00:00Z")).StatusCode);
    }

    [Fact]
    public async Task Migration_preserves_details_and_partial_unique_index_allows_only_one_active_product()
    {
        var refs = await References();
        await using var db = Db();
        await db.GetService<IMigrator>().MigrateAsync("20260924213105_CustomerIntegerIdentity");
        var order = new Order { CustomerId = refs.Customer, EmployeeId = (await db.Employees.FirstAsync()).EmployeeId, OrderDate = DateTime.UtcNow };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "OrderDetails" ("OrderId", "ProductId", "UnitPrice", "Quantity", "Discount", "Active")
            VALUES ({order.OrderId}, {refs.Products[0]}, 10, 2, 0, true),
                   ({order.OrderId}, {refs.Products[1]}, 20, 3, 0, false)
            """);
        var created = await db.Database.SqlQueryRaw<DateTime>("""
            SELECT "CreationDate" AS "Value" FROM "OrderDetails" WHERE "Active"
            """).SingleAsync();
        await db.Database.MigrateAsync();
        var details = await db.OrderDetails.IgnoreQueryFilters().OrderBy(x => x.ProductId).ToListAsync();
        Assert.Equal(2, details.Count);
        Assert.All(details, d => Assert.True(d.OrderDetailId > 0));
        Assert.Equal(created, details[0].CreationDate);
        Assert.Null(details[0].UpdatedDate);
        Assert.False(details[1].Active);
        var error = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "OrderDetails" ("OrderId", "ProductId", "UnitPrice", "Quantity", "Discount")
            VALUES ({order.OrderId}, {refs.Products[0]}, 10, 1, 0)
            """));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, error.SqlState);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "OrderDetails" ("OrderId", "ProductId", "UnitPrice", "Quantity", "Discount")
            VALUES ({order.OrderId}, {refs.Products[1]}, 20, 1, 0)
            """);
        Assert.Equal(3, await db.OrderDetails.IgnoreQueryFilters().CountAsync());
        var previousMaxId = details.Max(d => d.OrderDetailId);
        Assert.True(await db.OrderDetails.AnyAsync(x => x.OrderDetailId > previousMaxId));
    }

    [Fact]
    public async Task Database_conflict_rolls_back_new_header_and_all_details()
    {
        var refs = await References();
        await using var db = Db();
        var order = new Order
        {
            CustomerId = refs.Customer,
            EmployeeId = (await db.Employees.FirstAsync()).EmployeeId,
            OrderDate = DateTime.UtcNow,
            OrderDetails = new List<OrderDetail>
            {
                new() { ProductId = refs.Products[0], Quantity = 1, UnitPrice = 10 },
                new() { ProductId = refs.Products[0], Quantity = 2, UnitPrice = 10 }
            }
        };
        var error = await Assert.ThrowsAsync<Finanzauto.Application.Common.Exceptions.ApiException>(() =>
            new Finanzauto.Infrastructure.Orders.OrderStore(db).AddAsync(order, default));
        Assert.Equal(409, error.StatusCode);
        await using var verification = Db();
        Assert.Equal(0, await verification.Orders.IgnoreQueryFilters().CountAsync());
        Assert.Equal(0, await verification.OrderDetails.IgnoreQueryFilters().CountAsync());
    }
}
