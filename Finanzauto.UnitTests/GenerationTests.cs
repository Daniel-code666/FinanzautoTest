using System.ComponentModel.DataAnnotations;
using Finanzauto.Application.Catalog;

namespace Finanzauto.UnitTests;

public class GenerationTests
{
    [Theory]
    [InlineData("es-CO")]
    [InlineData("es-ES")]
    [InlineData("en-US")]
    public void Decimal_validation_is_independent_of_server_culture(string culture)
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new(culture);
            // New attributes avoid cached conversions masking culture-dependent failures.
            foreach (var property in new[] { typeof(ProductRequest).GetProperty("UnitPrice")!,
                typeof(ProductQuery).GetProperty("MinPrice")!, typeof(ProductQuery).GetProperty("MaxPrice")! })
            {
                var range = (RangeAttribute)Attribute.GetCustomAttribute(property, typeof(RangeAttribute))!;
                Assert.True(range.IsValid(12.34m));
                Assert.False(range.IsValid(-1m));
                Assert.False(range.IsValid(10000000000000000m));
            }
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = previous; }
    }

    [Theory]
    [InlineData(1, 0, 0)]
    [InlineData(101, 125, 987)]
    public void Generated_products_respect_count_references_prices_and_uniqueness(int count, int minCents, int maxCents)
    {
        var request = new GenerateProductsRequest
        {
            Count = count,
            CategoryIds = [10, 20],
            SupplierId = 7,
            MinPrice = minCents / 100m,
            MaxPrice = maxCents / 100m,
            NamePrefix = " Test "
        };
        var products = new RandomProductGenerator().Generate(request, Guid.NewGuid()).ToArray();
        Assert.Equal(count, products.Length);
        Assert.Equal(count, products.Select(p => p.ProductName).Distinct().Count());
        Assert.All(products, p =>
        {
            Assert.Contains(p.CategoryId, request.CategoryIds);
            Assert.Equal(7, p.SupplierId);
            Assert.InRange(p.UnitPrice, request.MinPrice, request.MaxPrice);
            Assert.Equal(p.UnitPrice, decimal.Round(p.UnitPrice, 2));
            Assert.StartsWith("Test-", p.ProductName);
            Assert.InRange(p.UnitsInStock, 0, 1000);
        });
        Assert.InRange(Math.Abs(products.Count(p => p.CategoryId == 10) - products.Count(p => p.CategoryId == 20)), 0, 1);
    }

    [Theory]
    [InlineData(0, 1, 2)]
    [InlineData(100001, 1, 2)]
    [InlineData(10, 3, 2)]
    public void Invalid_generation_requests_fail_validation(int count, int min, int max)
    {
        var request = new GenerateProductsRequest { Count = count, CategoryIds = [1], SupplierId = 1, MinPrice = min, MaxPrice = max };
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), [], true));
    }
}
