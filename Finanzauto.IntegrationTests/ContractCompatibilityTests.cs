using System.Text.Json;
using System.Text.Json.Nodes;
using Finanzauto.Application.Catalog;
using Finanzauto.Application.Identity;
using Finanzauto.Application.Partners;
using Finanzauto.Errors;

namespace Finanzauto.IntegrationTests;

public class ContractCompatibilityTests
{
    // Fixture captured from the record DTOs before their conversion to classes.
    [Theory]
    [InlineData(typeof(CatalogPage<int>))]
    [InlineData(typeof(CategoryResponse))]
    [InlineData(typeof(CategoryDetailResponse))]
    [InlineData(typeof(SupplierResponse))]
    [InlineData(typeof(ProductResponse))]
    [InlineData(typeof(ProductDetailResponse))]
    [InlineData(typeof(GenerationResponse))]
    [InlineData(typeof(PageResult<int>))]
    [InlineData(typeof(UserResponse))]
    [InlineData(typeof(RoleResponse))]
    [InlineData(typeof(LoginResponse))]
    [InlineData(typeof(IssuedToken))]
    [InlineData(typeof(SessionUser))]
    [InlineData(typeof(ProfileResponse))]
    [InlineData(typeof(BulkResponse<int>))]
    [InlineData(typeof(ApiErrorResponse))]
    public void Response_contract_preserves_previous_JSON(Type responseType)
    {
        var fixture = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ResponseContracts.json")))!;
        var expected = fixture[responseType.Name]!;
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var response = JsonSerializer.Deserialize(expected.ToJsonString(), responseType, options);
        Assert.NotNull(response);
        var actual = JsonSerializer.SerializeToNode(response, responseType, options);
        Assert.True(JsonNode.DeepEquals(expected, actual), $"JSON contract changed for {responseType.Name}");
    }
}
