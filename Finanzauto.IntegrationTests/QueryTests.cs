using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Finanzauto.Application.Catalog;
using Finanzauto.Application.Partners;
using Finanzauto.Domain.Entities;
using Finanzauto.Infrastructure.Catalog;
using Finanzauto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Finanzauto.IntegrationTests;

[Collection("PostgreSQL")]
public class QueryTests(PostgresFixture postgres) : ApiTest(postgres)
{
    [Theory]
    [InlineData("Id", false, new[] { 0, 1, 2, 3 })]
    [InlineData("Id", true, new[] { 3, 2, 1, 0 })]
    [InlineData("Name", false, new[] { 1, 2, 3, 0 })]
    [InlineData("Name", true, new[] { 0, 3, 2, 1 })]
    [InlineData("Price", false, new[] { 1, 2, 0, 3 })]
    [InlineData("Price", true, new[] { 3, 0, 2, 1 })]
    public async Task Product_sort_preserves_ties_across_pages(string sort, bool descending, int[] expectedOrder)
    {
        await using var db = Db();
        var category = new Category { CategoryName = "Sort" };
        var supplier = await db.Suppliers.FirstAsync();
        var names = new[] { "Zulu", "Alpha", "Alpha", "Bravo" };
        var prices = new[] { 20m, 10m, 10m, 30m };
        var products = new List<Product>();
        for (var i = 0; i < names.Length; i++)
        {
            var product = new Product
            {
                ProductName = names[i],
                UnitPrice = prices[i],
                Category = category,
                SupplierId = supplier.SupplierId
            };
            db.Products.Add(product);
            await db.SaveChangesAsync();
            products.Add(product);
        }
        for (var pageNumber = 1; pageNumber <= 4; pageNumber++)
        {
            var page = (await Admin.GetFromJsonAsync<CatalogPage<ProductResponse>>(
                $"/Products?sortBy={sort}&descending={descending}&page={pageNumber}&pageSize=1"))!;
            Assert.Equal(4, page.TotalCount);
            Assert.Equal(pageNumber, page.Page);
            Assert.Equal(1, page.PageSize);
            Assert.Equal(products[expectedOrder[pageNumber - 1]].ProductId, Assert.Single(page.Items).Id);
        }
        var empty = (await Admin.GetFromJsonAsync<CatalogPage<ProductResponse>>(
            $"/Products?sortBy={sort}&descending={descending}&page=5&pageSize=1"))!;
        Assert.Empty(empty.Items);
        Assert.Equal(4, empty.TotalCount);
    }

    [Fact]
    public async Task Product_filters_are_combined_before_count_and_pagination()
    {
        await using var db = Db();
        var category = new Category { CategoryName = "Filters" };
        var otherCategory = new Category { CategoryName = "Other" };
        var supplier = await db.Suppliers.FirstAsync();
        var otherSupplier = new Supplier { CompanyName = "Other" };
        Product Make() => new()
        {
            ProductName = "Cloud target",
            Category = category,
            Supplier = supplier,
            UnitPrice = 20,
            UnitsInStock = 0,
            Discontinued = true
        };
        var target = Make();
        var wrongName = Make(); wrongName.ProductName = "Unrelated";
        var tooCheap = Make(); tooCheap.UnitPrice = 9;
        var tooExpensive = Make(); tooExpensive.UnitPrice = 31;
        var inStock = Make(); inStock.UnitsInStock = 1;
        var continued = Make(); continued.Discontinued = false;
        var wrongCategory = Make(); wrongCategory.Category = otherCategory;
        var wrongSupplier = Make(); wrongSupplier.Supplier = otherSupplier;
        var inactive = Make(); inactive.Active = false;
        db.Products.AddRange(target, wrongName, tooCheap, tooExpensive, inStock, continued, wrongCategory, wrongSupplier, inactive);
        await db.SaveChangesAsync();
        var url = $"/Products?search=%20cLoUd%20&categoryId={category.CategoryId}&supplierId={supplier.SupplierId}&minPrice=10&maxPrice=30&inStock=false&discontinued=true&pageSize=1";
        var page = (await Admin.GetFromJsonAsync<CatalogPage<ProductResponse>>(url))!;
        Assert.Equal(1, page.TotalCount);
        Assert.Equal(target.ProductId, Assert.Single(page.Items).Id);
        var empty = (await Admin.GetFromJsonAsync<CatalogPage<ProductResponse>>(url + "&page=2"))!;
        Assert.Empty(empty.Items);
        Assert.Equal(1, empty.TotalCount);
        var missing = (await Admin.GetFromJsonAsync<CatalogPage<ProductResponse>>("/Products?search=missing"))!;
        Assert.Empty(missing.Items);
        Assert.Equal(0, missing.TotalCount);
    }

    [Fact]
    public async Task Products_with_inactive_references_are_excluded_from_count_list_and_detail()
    {
        await using var db = Db();
        var category = new Category { CategoryName = "Visible" };
        var hiddenCategory = new Category { CategoryName = "Hidden", Active = false };
        var supplier = await db.Suppliers.FirstAsync();
        var hiddenSupplier = new Supplier { CompanyName = "Hidden", Active = false };
        var visible = new Product { ProductName = "Visible", Category = category, Supplier = supplier };
        var hiddenByCategory = new Product { ProductName = "Hidden category", Category = hiddenCategory, Supplier = supplier };
        var hiddenBySupplier = new Product { ProductName = "Hidden supplier", Category = category, Supplier = hiddenSupplier };
        db.Products.AddRange(visible, hiddenByCategory, hiddenBySupplier);
        await db.SaveChangesAsync();
        var page = (await Admin.GetFromJsonAsync<CatalogPage<ProductResponse>>("/Products"))!;
        Assert.Equal(1, page.TotalCount);
        Assert.Equal(visible.ProductId, Assert.Single(page.Items).Id);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.GetAsync($"/Products/{hiddenByCategory.ProductId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.GetAsync($"/Products/{hiddenBySupplier.ProductId}")).StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Partner_list_filters_and_mapping_match_detail(bool suppliers)
    {
        await using var db = Db();
        var path = suppliers ? "/Suppliers" : "/Customers";
        var ids = new List<string>();
        for (var i = 0; i < 5; i++)
        {
            var country = i == 2 ? "Peru" : "Colombia";
            var city = i == 3 ? "Medellin" : "Bogota";
            var active = i != 4;
            if (suppliers)
            {
                var entity = new Supplier
                {
                    CompanyName = $"Company {i}",
                    ContactName = "Needle",
                    ContactTitle = "Manager",
                    Address = "Address",
                    City = city,
                    Region = "Region",
                    PostalCode = "110111",
                    Country = country,
                    Phone = "123",
                    Fax = "456",
                    HomePage = "https://example.test",
                    Active = active
                };
                db.Suppliers.Add(entity);
                await db.SaveChangesAsync();
                ids.Add(entity.SupplierId.ToString());
            }
            else
            {
                var entity = new Customer
                {
                    CustomerId = $"C{i}",
                    CompanyName = $"Company {i}",
                    ContactName = "Needle",
                    ContactTitle = "Manager",
                    Address = "Address",
                    City = city,
                    Region = "Region",
                    PostalCode = "110111",
                    Country = country,
                    Phone = "123",
                    Fax = "456",
                    Active = active
                };
                db.Customers.Add(entity);
                await db.SaveChangesAsync();
                ids.Add(entity.CustomerId);
            }
        }
        var query = "?search=%20nEeDlE%20&country=%20coLOmbia%20&city=%20boGota%20&pageSize=1";
        var page = (await Admin.GetFromJsonAsync<JsonElement>(path + query + "&page=2"));
        Assert.Equal(2, page.GetProperty("totalCount").GetInt32());
        var item = Assert.Single(page.GetProperty("items").EnumerateArray());
        var detail = await Admin.GetFromJsonAsync<JsonElement>(path + "/" + ids[1]);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(item.GetRawText()), JsonNode.Parse(detail.GetRawText())));
        Assert.Equal("Company 1", item.GetProperty("companyName").GetString());
        Assert.Equal("Needle", item.GetProperty("contactName").GetString());
        Assert.Equal("Manager", item.GetProperty("contactTitle").GetString());
        Assert.Equal("Address", item.GetProperty("address").GetString());
        Assert.Equal("Bogota", item.GetProperty("city").GetString());
        Assert.Equal("Region", item.GetProperty("region").GetString());
        Assert.Equal("110111", item.GetProperty("postalCode").GetString());
        Assert.Equal("Colombia", item.GetProperty("country").GetString());
        Assert.Equal("123", item.GetProperty("phone").GetString());
        Assert.Equal("456", item.GetProperty("fax").GetString());
        Assert.NotEqual(default, item.GetProperty("creationDate").GetDateTime());
        Assert.Equal(JsonValueKind.Null, item.GetProperty("updatedDate").ValueKind);
        if (suppliers) Assert.Equal("https://example.test", item.GetProperty("homePage").GetString());
        var empty = await Admin.GetFromJsonAsync<JsonElement>(path + query + "&page=3");
        Assert.Empty(empty.GetProperty("items").EnumerateArray());
        Assert.Equal(2, empty.GetProperty("totalCount").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.GetAsync(path + "/" + ids[4])).StatusCode);
    }

    [Fact]
    public async Task Product_query_sends_filters_and_pagination_to_PostgreSQL()
    {
        await using var source = Db();
        var commands = new List<string>();
        var options = new DbContextOptionsBuilder<FinanzautoDbContext>()
            .UseNpgsql(source.Database.GetConnectionString())
            .LogTo(commands.Add, new[] { RelationalEventId.CommandExecuted })
            .Options;
        await using var db = new FinanzautoDbContext(options);
        await new CatalogStore(db).ListProductsAsync(new ProductQuery
        {
            Search = "server",
            MinPrice = 10,
            Page = 2,
            PageSize = 5
        }, default);
        Assert.Equal(2, commands.Count); // Count and one projected page, no per-row queries.
        var pageCommand = commands.Single(sql => sql.Contains("LIMIT"));
        Assert.Contains("OFFSET", pageCommand);
        Assert.Contains("WHERE", pageCommand);
        Assert.Contains("SearchName", pageCommand);
        Assert.Contains("UnitPrice", pageCommand);
        Assert.DoesNotContain("Picture", pageCommand);
    }
}
