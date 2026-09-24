using Finanzauto.Application.Common.Exceptions;
using Finanzauto.Application.Partners;
using Finanzauto.Domain.Entities;
using Moq;

namespace Finanzauto.UnitTests;

public class PartnerTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public async Task Both_bulk_endpoints_reject_out_of_range_batches(int count)
    {
        var store = new Mock<IPartnerStore>(MockBehavior.Strict);
        var service = new PartnerService(store.Object);
        Assert.Equal(400, (await Assert.ThrowsAsync<ApiException>(() =>
            service.CreateSuppliersAsync(new SupplierRequest[count], default))).StatusCode);
        Assert.Equal(400, (await Assert.ThrowsAsync<ApiException>(() =>
            service.CreateCustomersAsync(new CustomerRequest[count], default))).StatusCode);
        store.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Invalid_or_null_items_are_rejected_before_writing(bool nullItem)
    {
        var store = new Mock<IPartnerStore>(MockBehavior.Strict);
        var service = new PartnerService(store.Object);
        Assert.Equal(400, (await Assert.ThrowsAsync<ApiException>(() =>
            service.CreateSuppliersAsync([nullItem ? null! : new SupplierRequest()], default))).StatusCode);
        Assert.Equal(400, (await Assert.ThrowsAsync<ApiException>(() =>
            service.CreateCustomersAsync([nullItem ? null! : new CustomerRequest()], default))).StatusCode);
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Maximum_batch_is_accepted_for_both_partners()
    {
        var store = new Mock<IPartnerStore>();
        var service = new PartnerService(store.Object);
        var suppliers = Enumerable.Range(1, 1000).Select(i => new SupplierRequest { CompanyName = $" Supplier {i} " }).ToArray();
        var customers = Enumerable.Range(1, 1000).Select(i => new CustomerRequest { CompanyName = $" Customer {i} " }).ToArray();
        Assert.Equal(1000, (await service.CreateSuppliersAsync(suppliers, default)).CreatedCount);
        var result = await service.CreateCustomersAsync(customers, default);
        Assert.Equal(1000, result.CreatedCount);
        Assert.Equal(0, result.Items[0].Id);
        Assert.Equal("Customer 1", result.Items[0].CompanyName);
        store.Verify(s => s.AddCustomersAsync(It.Is<IReadOnlyList<Customer>>(x => x.Count == 1000), default), Times.Once);
        store.Verify(s => s.AddSuppliersAsync(It.Is<IReadOnlyList<Supplier>>(x => x.Count == 1000), default), Times.Once);
    }
}

