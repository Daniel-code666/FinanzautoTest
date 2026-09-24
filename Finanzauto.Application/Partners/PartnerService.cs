using System.ComponentModel.DataAnnotations;
using Finanzauto.Application.Catalog;
using Finanzauto.Application.Common.Exceptions;
using Finanzauto.Domain.Entities;

namespace Finanzauto.Application.Partners;

public sealed class PartnerService(IPartnerStore store) : IPartnerService
{
    public Task<CatalogPage<SupplierDetailResponse>> ListSuppliersAsync(PartnerQuery query, CancellationToken ct)
        => store.ListSuppliersAsync(query, ct);

    public Task<CatalogPage<CustomerResponse>> ListCustomersAsync(PartnerQuery query, CancellationToken ct)
        => store.ListCustomersAsync(query, ct);

    private async Task<Supplier> SupplierAsync(int id, CancellationToken ct)
        => await store.FindSupplierAsync(id, ct) ?? throw new ApiException(404, "Proveedor no encontrado.");

    private async Task<Customer> CustomerAsync(int id, CancellationToken ct)
        => await store.FindCustomerAsync(id, ct) ?? throw new ApiException(404, "Cliente no encontrado.");

    public async Task<SupplierDetailResponse> GetSupplierAsync(int id, CancellationToken ct)
    {
        var supplier = await SupplierAsync(id, ct);
        return supplier.ToResponse();
    }

    public async Task<CustomerResponse> GetCustomerAsync(int id, CancellationToken ct)
    {
        var customer = await CustomerAsync(id, ct);
        return customer.ToResponse();
    }

    public async Task<SupplierDetailResponse> CreateSupplierAsync(SupplierRequest request, CancellationToken ct)
    {
        var result = await CreateSuppliersAsync([request], ct);
        return result.Items[0];
    }

    public async Task<CustomerResponse> CreateCustomerAsync(CustomerRequest request, CancellationToken ct)
    {
        var result = await CreateCustomersAsync([request], ct);
        return result.Items[0];
    }

    public async Task<SupplierDetailResponse> UpdateSupplierAsync(int id, SupplierRequest request, CancellationToken ct)
    {
        var entity = await SupplierAsync(id, ct);
        PartnerMapping.Apply(entity, request);
        await store.SaveAsync(ct);
        return entity.ToResponse();
    }

    public async Task<CustomerResponse> UpdateCustomerAsync(int id, ContactRequest request, CancellationToken ct)
    {
        var entity = await CustomerAsync(id, ct);
        PartnerMapping.Apply(entity, request);
        await store.SaveAsync(ct);
        return entity.ToResponse();
    }

    public Task DeleteSupplierAsync(int id, CancellationToken ct)
       => store.DeleteSupplierAsync(id, ct);

    public Task DeleteCustomerAsync(int id, CancellationToken ct)
        => store.DeleteCustomerAsync(id, ct);

    public async Task<BulkResponse<SupplierDetailResponse>> CreateSuppliersAsync(SupplierRequest[] requests, CancellationToken ct)
    {
        ValidateBatch(requests);
        var entities = requests.Select(request =>
        {
            var entity = new Supplier();
            PartnerMapping.Apply(entity, request);
            return entity;
        }).ToArray();
        await store.AddSuppliersAsync(entities, ct);
        return new BulkResponse<SupplierDetailResponse>
        {
            CreatedCount = entities.Length,
            Items = entities.Select(x => x.ToResponse()).ToArray()
        };
    }

    public async Task<BulkResponse<CustomerResponse>> CreateCustomersAsync(CustomerRequest[] requests, CancellationToken ct)
    {
        ValidateBatch(requests);
        var entities = requests.Select(request =>
        {
            var entity = new Customer();
            PartnerMapping.Apply(entity, request);
            return entity;
        }).ToArray();

        await store.AddCustomersAsync(entities, ct);

        return new BulkResponse<CustomerResponse>
        {
            CreatedCount = entities.Length,
            Items = [.. entities.Select(x => x.ToResponse())]
        };
    }

    private static void ValidateBatch<T>(T[] requests)
    {
        if (requests == null || requests.Length < 1 || requests.Length > 1000)
            throw new ApiException(400, "El arreglo debe contener entre 1 y 1000 objetos.");

        for (var i = 0; i < requests.Length; i++)
        {
            var request = requests[i] ?? throw new ApiException(400, $"El objeto en la posición {i} es nulo.");

            var errors = new List<ValidationResult>();
            if (!Validator.TryValidateObject(request, new ValidationContext(request), errors, true))
            {
                throw new ApiException(400, $"Objeto {i}: {string.Join(" ", errors.Select(x => x.ErrorMessage))}");
            }
        }
    }
}
