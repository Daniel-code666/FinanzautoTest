using Finanzauto.Application.Catalog;
using Finanzauto.Domain.Entities;

namespace Finanzauto.Application.Partners;

public interface IPartnerStore
{
    Task<CatalogPage<SupplierDetailResponse>> ListSuppliersAsync(PartnerQuery query, CancellationToken ct);
    Task<CatalogPage<CustomerResponse>> ListCustomersAsync(PartnerQuery query, CancellationToken ct);
    Task<Supplier?> FindSupplierAsync(int id, CancellationToken ct);
    Task<Customer?> FindCustomerAsync(string id, CancellationToken ct);
    Task AddSuppliersAsync(IReadOnlyList<Supplier> items, CancellationToken ct);
    Task AddCustomersAsync(IReadOnlyList<Customer> items, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
    Task DeleteSupplierAsync(int id, CancellationToken ct);
    Task DeleteCustomerAsync(string id, CancellationToken ct);
}
public interface IPartnerService
{
    Task<CatalogPage<SupplierDetailResponse>> ListSuppliersAsync(PartnerQuery query, CancellationToken ct);
    Task<CatalogPage<CustomerResponse>> ListCustomersAsync(PartnerQuery query, CancellationToken ct);
    Task<SupplierDetailResponse> GetSupplierAsync(int id, CancellationToken ct);
    Task<CustomerResponse> GetCustomerAsync(string id, CancellationToken ct);
    Task<SupplierDetailResponse> CreateSupplierAsync(SupplierRequest request, CancellationToken ct);
    Task<CustomerResponse> CreateCustomerAsync(CustomerRequest request, CancellationToken ct);
    Task<SupplierDetailResponse> UpdateSupplierAsync(int id, SupplierRequest request, CancellationToken ct);
    Task<CustomerResponse> UpdateCustomerAsync(string id, ContactRequest request, CancellationToken ct);
    Task DeleteSupplierAsync(int id, CancellationToken ct);
    Task DeleteCustomerAsync(string id, CancellationToken ct);
    Task<BulkResponse<SupplierDetailResponse>> CreateSuppliersAsync(SupplierRequest[] requests, CancellationToken ct);
    Task<BulkResponse<CustomerResponse>> CreateCustomersAsync(CustomerRequest[] requests, CancellationToken ct);
}

