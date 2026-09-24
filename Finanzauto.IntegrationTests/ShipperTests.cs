using System.Net;
using System.Net.Http.Json;
using Finanzauto.Application.Catalog;
using Finanzauto.Application.Shippers;
using Finanzauto.Domain.Entities;
using Finanzauto.Errors;
using Finanzauto.Infrastructure.Orders;
using Microsoft.EntityFrameworkCore;

namespace Finanzauto.IntegrationTests;

[Collection("PostgreSQL")]
public sealed class ShipperTests(PostgresFixture postgres) : ApiTest(postgres)
{
    private static async Task<ShipperResponse> Create(HttpClient client, string name, string? phone = null)
    {
        using var response = await client.PostAsJsonAsync("/Shippers", new { companyName = name, phone });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ShipperResponse>())!;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CRUD_filters_pagination_and_audit_work_for_authenticated_roles(bool administrator)
    {
        var (userClient, _) = await Register();
        using (userClient)
        {
            var client = administrator ? Admin : userClient;
            var first = await Create(client, " Transport One ", " 12345 ");
            var second = await Create(client, "Transport Two");
            await Create(client, "Unrelated", "67890");
            Assert.True(first.Id > 0);
            Assert.Equal("Transport One", first.CompanyName);
            Assert.Equal("12345", first.Phone);
            Assert.NotEqual(default, first.CreationDate);
            Assert.Null(first.UpdatedDate);
            var page = (await client.GetFromJsonAsync<CatalogPage<ShipperResponse>>("/Shippers?search=%20tRaNsPoRt%20&page=2&pageSize=1"))!;
            Assert.Equal(2, page.TotalCount);
            Assert.Equal(second.Id, Assert.Single(page.Items).Id);
            var phone = (await client.GetFromJsonAsync<CatalogPage<ShipperResponse>>("/Shippers?search=12345"))!;
            Assert.Equal(first.Id, Assert.Single(phone.Items).Id);
            using var update = await client.PutAsJsonAsync($"/Shippers/{first.Id}", new { companyName = " Updated " });
            Assert.Equal(HttpStatusCode.OK, update.StatusCode);
            var detail = (await client.GetFromJsonAsync<ShipperResponse>($"/Shippers/{first.Id}"))!;
            Assert.Equal("Updated", detail.CompanyName);
            Assert.Null(detail.Phone);
            Assert.Equal(first.CreationDate, detail.CreationDate);
            Assert.NotNull(detail.UpdatedDate);
            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/Shippers/{first.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/Shippers/{first.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/Shippers/{first.Id}", new { companyName = "Again" })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/Shippers/{first.Id}")).StatusCode);
            Assert.Equal(2, (await client.GetFromJsonAsync<CatalogPage<ShipperResponse>>("/Shippers"))!.TotalCount);
            await using var db = Db();
            Assert.False((await db.Shippers.IgnoreQueryFilters().SingleAsync(x => x.ShipperId == first.Id)).Active);
        }
    }

    [Fact]
    public async Task Active_order_blocks_deactivation_but_inactive_order_is_preserved()
    {
        var shipper = await Create(Admin, "Assigned");
        var unrelated = await Create(Admin, "Unassigned");
        int orderId;
        await using (var db = Db())
        {
            var order = new Order
            {
                Customer = new Customer { CompanyName = "Customer" },
                EmployeeId = (await db.Employees.FirstAsync()).EmployeeId,
                ShipVia = shipper.Id,
                OrderDate = DateTime.UtcNow
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            orderId = order.OrderId;
        }
        using var blocked = await Admin.DeleteAsync($"/Shippers/{shipper.Id}");
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        var error = (await blocked.Content.ReadFromJsonAsync<ApiErrorResponse>())!;
        Assert.Equal(409, error.HttpCode);
        Assert.Contains("pedidos activos", error.Description);
        Assert.Equal(HttpStatusCode.OK, (await Admin.GetAsync($"/Shippers/{shipper.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Admin.DeleteAsync($"/Shippers/{unrelated.Id}")).StatusCode);
        await using (var db = Db())
            await new OrderStore(db).DeactivateAsync(orderId, default);
        Assert.Equal(HttpStatusCode.NoContent, (await Admin.DeleteAsync($"/Shippers/{shipper.Id}")).StatusCode);
        await using var verification = Db();
        var savedOrder = await verification.Orders.IgnoreQueryFilters().SingleAsync(x => x.OrderId == orderId);
        Assert.False(savedOrder.Active);
        Assert.Equal(shipper.Id, savedOrder.ShipVia);
        Assert.False((await verification.Shippers.IgnoreQueryFilters().SingleAsync(x => x.ShipperId == shipper.Id)).Active);
    }

    [Fact]
    public async Task Endpoints_require_authentication_and_validate_requests()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Anonymous.GetAsync("/Shippers")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Anonymous.GetAsync("/Shippers/1")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Anonymous.PostAsJsonAsync("/Shippers", new { companyName = "Test" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Anonymous.PutAsJsonAsync("/Shippers/1", new { companyName = "Test" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Anonymous.DeleteAsync("/Shippers/1")).StatusCode);
        foreach (var name in new[] { "", "   ", new string('a', 201) })
            Assert.Equal(HttpStatusCode.BadRequest, (await Admin.PostAsJsonAsync("/Shippers", new { companyName = name })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Admin.PostAsJsonAsync("/Shippers", new { companyName = "Valid", phone = new string('1', 31) })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Admin.GetAsync("/Shippers?page=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Admin.GetAsync("/Shippers?pageSize=101")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.GetAsync($"/Shippers/{int.MaxValue}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.PutAsJsonAsync($"/Shippers/{int.MaxValue}", new { companyName = "Valid" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.DeleteAsync($"/Shippers/{int.MaxValue}")).StatusCode);
    }
}
