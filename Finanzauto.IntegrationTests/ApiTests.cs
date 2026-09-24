using System.Net;
using System.Net.Http.Json;
using Finanzauto.Application.Catalog;
using Finanzauto.Application.Identity;
using Finanzauto.Application.Partners;
using Finanzauto.Errors;
using Microsoft.EntityFrameworkCore;

namespace Finanzauto.IntegrationTests;

[Collection("PostgreSQL")]
public sealed class ApiTests(PostgresFixture postgres) : ApiTest(postgres)
{
    [Theory]
    [InlineData("Register")]
    [InlineData("CreateUser")]
    [InlineData("UpdateUser")]
    [InlineData("Profile")]
    public async Task Email_is_normalized_and_login_and_duplicates_use_the_same_value(string operation)
    {
        var email = "MixedCase" + Guid.NewGuid().ToString("N") + "@Example.Test";
        int userId;
        if (operation == "Register" || operation == "CreateUser")
        {
            var path = operation == "Register" ? "/UserAdministration/Register" : "/UserAdministration/Users";
            var response = operation == "Register"
                ? await Anonymous.PostAsJsonAsync(path,
                    new { firstName = "Email", lastName = "Test", email, password = Factory.Password })
                : await Admin.PostAsJsonAsync(path,
                    new { firstName = "Email", lastName = "Test", email, password = Factory.Password, roleId = 2 });
            using (response)
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                userId = (await response.Content.ReadFromJsonAsync<UserResponse>())!.Id;
            }
        }
        else
        {
            var (client, user) = await Register();
            using (client)
            {
                userId = user.Id;
                using var response = operation == "Profile"
                    ? await client.PutAsJsonAsync("/Profile", new { firstName = "Email", lastName = "Test", email })
                    : await Admin.PutAsJsonAsync($"/UserAdministration/Users/{userId}",
                        new { firstName = "Email", lastName = "Test", email, roleId = 2 });
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                await Error(await Anonymous.PostAsJsonAsync("/Login",
                    new { email = user.Email, password = Factory.Password }), HttpStatusCode.Unauthorized);
            }
        }
        await using var db = Db();
        var saved = await db.Employees.SingleAsync(e => e.EmployeeId == userId);
        Assert.Equal(email, saved.Email);
        Assert.Equal(email.ToUpperInvariant(), saved.NormalizedEmail);
        using var login = await Login(email.ToLowerInvariant(), Factory.Password);
        await Error(await Anonymous.PostAsJsonAsync("/UserAdministration/Register",
            new { firstName = "Duplicate", lastName = "Email", email = email.ToLowerInvariant(), password = Factory.Password }),
            HttpStatusCode.Conflict);
    }

    private static async Task<T> ReadResponse<T>(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.Created)
    {
        using (response)
        {
            Assert.Equal(expected, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<T>())!;
        }
    }

    private async Task<(int Category, int Supplier)> References(HttpClient client)
    {
        var category = await ReadResponse<CategoryDetailResponse>(await client.PostAsJsonAsync("/Category",
            new { categoryName = "SERVIDORES", picture = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 } }), HttpStatusCode.OK);
        var supplier = await ReadResponse<SupplierDetailResponse>(await client.PostAsJsonAsync("/Suppliers", new { companyName = "Supplier" }));
        return (category.Id, supplier.Id);
    }

    private static async Task Error(HttpResponseMessage response, HttpStatusCode expected)
    {
        using (response)
        {
            Assert.Equal(expected, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            var error = (await response.Content.ReadFromJsonAsync<ApiErrorResponse>())!;
            Assert.Equal((int)expected, error.HttpCode);
            Assert.False(string.IsNullOrWhiteSpace(error.Description));
            Assert.False(string.IsNullOrWhiteSpace(error.Exception));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Authentication_rejects_malformed_or_altered_tokens(bool alterSignature)
    {
        var token = "invalid-token";
        if (alterSignature)
        {
            var parts = Admin.DefaultRequestHeaders.Authorization!.Parameter!.Split('.');
            parts[2] = (parts[2][0] == 'A' ? "B" : "A") + parts[2].Substring(1);
            token = string.Join(".", parts);
        }

        using var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        await Error(await client.GetAsync("/Profile"), HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Authentication_registration_and_profile_permissions()
    {
        await Error(await Anonymous.GetAsync("/Profile"), HttpStatusCode.Unauthorized);
        await Error(await Anonymous.PostAsJsonAsync("/Login",
            new { email = Factory.AdminEmail, password = "wrong-password" }), HttpStatusCode.Unauthorized);
        var (client, user) = await Register();
        using (client)
        {
            var profile = (await client.GetFromJsonAsync<ProfileResponse>("/Profile"))!;
            Assert.Equal(user.Id, profile.Id);
            Assert.Equal("User", profile.RoleName);
            var updated = await client.PutAsJsonAsync("/Profile",
                new { firstName = "Changed", lastName = "User", email = user.Email, city = "Bogota" });
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
            Assert.Equal("Changed", (await updated.Content.ReadFromJsonAsync<ProfileResponse>())!.FirstName);
            await Error(await client.PutAsJsonAsync("/Profile",
                new { firstName = "Changed", lastName = "User", email = user.Email, roleId = 1 }), HttpStatusCode.BadRequest);
            await Error(await client.PutAsJsonAsync("/Profile/Password",
                new { currentPassword = "incorrect", newPassword = "ReplacementPassword123!" }), HttpStatusCode.BadRequest);
            Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync("/Profile/Password",
                new { currentPassword = Factory.Password, newPassword = "ReplacementPassword123!" })).StatusCode);
            await Error(await Anonymous.PostAsJsonAsync("/Login",
                new { email = user.Email, password = Factory.Password }), HttpStatusCode.Unauthorized);
            using var changedLogin = await Login(user.Email, "ReplacementPassword123!");
            Assert.Equal(HttpStatusCode.OK, (await changedLogin.GetAsync("/Profile")).StatusCode);
        }
    }

    [Fact]
    public async Task User_cannot_manage_customers_users_or_roles_but_can_read_users_and_customers()
    {
        var (client, user) = await Register();
        using (client)
        {
            var customer = await ReadResponse<CustomerResponse>(await Admin.PostAsJsonAsync("/Customers",
                new { companyName = "Customer" }), HttpStatusCode.OK);
            foreach (var path in new[] { "/Customers", $"/Customers/{customer.Id}", "/UserAdministration/Users", $"/UserAdministration/Users/{user.Id}" })
                Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
            await Error(await client.PostAsJsonAsync("/Customers", new { companyName = "Other" }), HttpStatusCode.Forbidden);
            await Error(await client.PostAsJsonAsync("/Customers/Bulk", new[] { new { companyName = "Other" } }), HttpStatusCode.Forbidden);
            await Error(await client.PutAsJsonAsync($"/Customers/{customer.Id}", new { companyName = "Other" }), HttpStatusCode.Forbidden);
            await Error(await client.DeleteAsync($"/Customers/{customer.Id}"), HttpStatusCode.Forbidden);
            await Error(await client.PostAsJsonAsync("/UserAdministration/Users",
                new { firstName = "A", lastName = "B", email = "new@example.test", password = Factory.Password, roleId = 1 }), HttpStatusCode.Forbidden);
            await Error(await client.PutAsJsonAsync($"/UserAdministration/Users/{user.Id}",
                new { firstName = "A", lastName = "B", email = user.Email, roleId = 1 }), HttpStatusCode.Forbidden);
            await Error(await client.DeleteAsync($"/UserAdministration/Users/{user.Id}"), HttpStatusCode.Forbidden);
            await Error(await client.GetAsync("/UserAdministration/Roles"), HttpStatusCode.Forbidden);
            await Error(await client.GetAsync("/UserAdministration/Roles/1"), HttpStatusCode.Forbidden);
            await Error(await client.PostAsJsonAsync("/UserAdministration/Roles", new { name = "Other" }), HttpStatusCode.Forbidden);
            await Error(await client.PutAsJsonAsync("/UserAdministration/Roles/1", new { name = "Other" }), HttpStatusCode.Forbidden);
            await Error(await client.DeleteAsync("/UserAdministration/Roles/1"), HttpStatusCode.Forbidden);
            Assert.Equal("Customer", (await Admin.GetFromJsonAsync<CustomerResponse>($"/Customers/{customer.Id}"))!.CompanyName);
            Assert.Equal(HttpStatusCode.OK, (await Admin.PutAsJsonAsync($"/Customers/{customer.Id}", new { companyName = "Updated" })).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await Admin.DeleteAsync($"/Customers/{customer.Id}")).StatusCode);
            await using var db = Db();
            Assert.False((await db.Customers.IgnoreQueryFilters().SingleAsync(c => c.CustomerId == customer.Id)).Active);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Anonymous_password_reset_accepts_email_or_id_and_changes_login(bool useId)
    {
        var (client, user) = await Register();
        using (client)
        {
            Assert.Null(Anonymous.DefaultRequestHeaders.Authorization);
            var path = "/UserAdministration/Users/ResetPassword";
            if (useId) path += $"?id={user.Id}";
            var newPassword = "AnonymousResetPassword2026!";
            using var response = await Anonymous.PutAsJsonAsync(path, new { email = user.Email, password = newPassword });
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await Error(await Anonymous.PostAsJsonAsync("/Login",
                new { email = user.Email, password = Factory.Password }), HttpStatusCode.Unauthorized);
            using var newSession = await Login(user.Email, newPassword);
            var profile = (await newSession.GetFromJsonAsync<ProfileResponse>("/Profile"))!;
            Assert.Equal(user.Id, profile.Id);
        }
    }

    [Fact]
    public async Task Administrator_can_manage_roles_and_users_and_deactivation_invalidates_JWT()
    {
        var role = await ReadResponse<RoleResponse>(await Admin.PostAsJsonAsync("/UserAdministration/Roles", new { name = "Auditor" }), HttpStatusCode.OK);
        var employee = await ReadResponse<UserResponse>(await Admin.PostAsJsonAsync("/UserAdministration/Users",
            new { firstName = "Audit", lastName = "User", email = "audit@example.test", password = Factory.Password, roleId = role.Id }), HttpStatusCode.OK);
        using var customUser = await Login(employee.Email, Factory.Password);
        Assert.Equal(HttpStatusCode.OK, (await customUser.GetAsync("/Customers")).StatusCode);
        await Error(await customUser.PostAsJsonAsync("/Customers", new { companyName = "X" }), HttpStatusCode.Forbidden);
        Assert.Equal(HttpStatusCode.OK, (await Admin.PutAsJsonAsync($"/UserAdministration/Users/{employee.Id}",
            new { firstName = "Modified", lastName = "User", email = employee.Email, roleId = role.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Admin.DeleteAsync($"/UserAdministration/Users/{employee.Id}")).StatusCode);
        await Error(await customUser.GetAsync("/Profile"), HttpStatusCode.Unauthorized);
        Assert.Equal(HttpStatusCode.NoContent, (await Admin.PostAsync($"/UserAdministration/Users/{employee.Id}/Reactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await customUser.GetAsync("/Profile")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Admin.DeleteAsync($"/UserAdministration/Roles/{role.Id}")).StatusCode);
        await Error(await customUser.GetAsync("/Profile"), HttpStatusCode.Unauthorized);
        await Error(await Admin.PostAsync($"/UserAdministration/Users/{employee.Id}/Reactivate", null), HttpStatusCode.Conflict);
        Assert.Equal(HttpStatusCode.NoContent, (await Admin.PostAsync($"/UserAdministration/Roles/{role.Id}/Reactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await customUser.GetAsync("/Profile")).StatusCode);
        await Error(await customUser.PostAsync($"/UserAdministration/Users/{employee.Id}/Reactivate", null), HttpStatusCode.Forbidden);
        await Error(await customUser.PostAsync($"/UserAdministration/Roles/{role.Id}/Reactivate", null), HttpStatusCode.Forbidden);
        await Error(await Admin.PostAsync($"/UserAdministration/Users/{int.MaxValue}/Reactivate", null), HttpStatusCode.NotFound);
        await Error(await Admin.PostAsync($"/UserAdministration/Roles/{int.MaxValue}/Reactivate", null), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Product_CRUD_filters_pagination_picture_soft_delete_and_audit()
    {
        var (client, _) = await Register();
        using (client)
        {
            var (category, supplier) = await References(client);
            var ids = new List<int>();
            foreach (var price in new[] { 10m, 20m, 30m, 40m })
            {
                var product = await ReadResponse<ProductDetailResponse>(await client.PostAsJsonAsync("/Products",
                    new { productName = $"Server {price}", categoryId = category, supplierId = supplier, unitPrice = price, unitsInStock = 5 }));
                ids.Add(product.Product.Id);
            }
            var detail = (await client.GetFromJsonAsync<ProductDetailResponse>($"/Products/{ids[0]}"))!;
            Assert.Equal("image/png", detail.Category.PictureContentType);
            Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, detail.Category.Picture);
            var page = (await client.GetFromJsonAsync<CatalogPage<ProductResponse>>(
                $"/Products?search=server&categoryId={category}&supplierId={supplier}&minPrice=15&maxPrice=35&inStock=true&sortBy=Price&page=2&pageSize=1"))!;
            Assert.Equal(2, page.TotalCount);
            Assert.Equal(30m, Assert.Single(page.Items).UnitPrice);
            await using var db = Db();
            var original = await db.Products.AsNoTracking().SingleAsync(p => p.ProductId == ids[0]);
            Assert.NotEqual(default, original.CreationDate);
            Assert.Null(original.UpdatedDate);
            Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/Products/{ids[0]}",
                new { productName = "Updated server", categoryId = category, supplierId = supplier, unitPrice = 15m })).StatusCode);
            var updated = await db.Products.AsNoTracking().SingleAsync(p => p.ProductId == ids[0]);
            Assert.Equal(original.CreationDate, updated.CreationDate);
            Assert.NotNull(updated.UpdatedDate);
            Assert.True(updated.UpdatedDate >= original.CreationDate);
            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/Products/{ids[0]}")).StatusCode);
            await Error(await client.GetAsync($"/Products/{ids[0]}"), HttpStatusCode.NotFound);
            Assert.False((await db.Products.IgnoreQueryFilters().AsNoTracking().SingleAsync(p => p.ProductId == ids[0])).Active);
            Assert.Equal(3, (await client.GetFromJsonAsync<CatalogPage<ProductResponse>>("/Products"))!.TotalCount);
            await Error(await client.GetAsync("/Products?page=0"), HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task Partner_bulk_generates_ids_and_invalid_batch_preserves_existing_data()
    {
        await using var initialDb = Db();
        var initialSuppliers = await initialDb.Suppliers.CountAsync();
        var suppliers = await ReadResponse<BulkResponse<SupplierDetailResponse>>(await Admin.PostAsJsonAsync("/Suppliers/Bulk",
            new[] { new { companyName = "One" }, new { companyName = "Two" } }));
        Assert.Equal(2, suppliers.CreatedCount);
        var customers = await ReadResponse<BulkResponse<CustomerResponse>>(await Admin.PostAsJsonAsync("/Customers/Bulk",
            new[] { new { companyName = "One" }, new { companyName = "Two" } }), HttpStatusCode.OK);
        Assert.Equal(2, customers.CreatedCount);
        Assert.All(customers.Items, c => Assert.True(c.Id > 0));
        Assert.Equal(2, customers.Items.Select(c => c.Id).Distinct().Count());
        await Error(await Admin.PostAsJsonAsync("/Customers/Bulk",
            new[] { new { companyName = "New" }, new { companyName = "" } }), HttpStatusCode.BadRequest);
        await Error(await Admin.PostAsJsonAsync("/Suppliers/Bulk",
            new[] { new { companyName = "Valid" }, new { companyName = "" } }), HttpStatusCode.BadRequest);
        await using var db = Db();
        Assert.Equal(2, await db.Customers.CountAsync());
        Assert.Equal(initialSuppliers + 2, await db.Suppliers.CountAsync());
        Assert.False(await db.Customers.AnyAsync(c => c.CompanyName == "New"));
        var supplierId = suppliers.Items[0].Id;
        Assert.Equal(HttpStatusCode.OK, (await Admin.PutAsJsonAsync($"/Suppliers/{supplierId}", new { companyName = "Updated" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Admin.DeleteAsync($"/Suppliers/{supplierId}")).StatusCode);
        Assert.False((await db.Suppliers.IgnoreQueryFilters().SingleAsync(s => s.SupplierId == supplierId)).Active);
    }

    [Fact]
    [Trait("Category", "Bulk")]
    public async Task Generate_100000_products_with_EF_batches_and_audit()
    {
        var (category, supplier) = await References(Admin);
        var cloud = await ReadResponse<CategoryDetailResponse>(await Admin.PostAsJsonAsync("/Category", new { categoryName = "CLOUD" }), HttpStatusCode.OK);
        var result = await ReadResponse<GenerationResponse>(await Admin.PostAsJsonAsync("/Product",
            new { count = 100000, categoryIds = new[] { category, cloud.Id }, supplierId = supplier, minPrice = 10m, maxPrice = 20m }));
        Assert.Equal(100000, result.CreatedCount);
        await using var db = Db();
        Assert.Equal(100000, await db.Products.CountAsync());
        Assert.Equal(50000, await db.Products.CountAsync(p => p.CategoryId == category));
        Assert.Equal(50000, await db.Products.CountAsync(p => p.CategoryId == cloud.Id));
        Assert.Equal(100000, await db.Products.Select(p => p.ProductName).Distinct().CountAsync());
        Assert.False(await db.Products.AnyAsync(p => p.SupplierId != supplier || p.UnitPrice < 10 || p.UnitPrice > 20 ||
            p.CreationDate == default || p.UpdatedDate != null));
        await Error(await Admin.PostAsJsonAsync("/Product",
            new { count = 10, categoryIds = new[] { int.MaxValue }, supplierId = supplier }), HttpStatusCode.Conflict);
        Assert.Equal(100000, await db.Products.CountAsync());
    }
}
