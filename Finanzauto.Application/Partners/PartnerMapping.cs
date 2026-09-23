using System.Linq.Expressions;
using Finanzauto.Domain.Entities;

namespace Finanzauto.Application.Partners;

public static class PartnerMapping
{
    public static readonly Expression<Func<Supplier, SupplierDetailResponse>> SupplierProjection =
        x => new SupplierDetailResponse
        {
            Id = x.SupplierId,
            CompanyName = x.CompanyName,
            ContactName = x.ContactName,
            ContactTitle = x.ContactTitle,
            Address = x.Address,
            City = x.City,
            Region = x.Region,
            PostalCode = x.PostalCode,
            Country = x.Country,
            Phone = x.Phone,
            Fax = x.Fax,
            HomePage = x.HomePage,
            CreationDate = x.CreationDate, UpdatedDate = x.UpdatedDate
        };
    private static readonly Func<Supplier, SupplierDetailResponse> MapSupplier = SupplierProjection.Compile();
    public static SupplierDetailResponse ToResponse(this Supplier entity) => MapSupplier(entity);

    public static void Apply(Supplier entity, SupplierRequest request)
    {
        entity.CompanyName = request.CompanyName.Trim();
        entity.ContactName = request.ContactName?.Trim();
        entity.ContactTitle = request.ContactTitle?.Trim();
        entity.Address = request.Address?.Trim();
        entity.City = request.City?.Trim();
        entity.Region = request.Region?.Trim();
        entity.PostalCode = request.PostalCode?.Trim();
        entity.Country = request.Country?.Trim();
        entity.Phone = request.Phone?.Trim();
        entity.Fax = request.Fax?.Trim();
        entity.HomePage = request.HomePage?.Trim();
    }
    public static readonly Expression<Func<Customer, CustomerResponse>> CustomerProjection =
        x => new CustomerResponse
        {
            Id = x.CustomerId,
            CompanyName = x.CompanyName,
            ContactName = x.ContactName,
            ContactTitle = x.ContactTitle,
            Address = x.Address,
            City = x.City,
            Region = x.Region,
            PostalCode = x.PostalCode,
            Country = x.Country,
            Phone = x.Phone,
            Fax = x.Fax,
            CreationDate = x.CreationDate, UpdatedDate = x.UpdatedDate
        };
    private static readonly Func<Customer, CustomerResponse> MapCustomer = CustomerProjection.Compile();
    public static CustomerResponse ToResponse(this Customer entity) => MapCustomer(entity);

    public static void Apply(Customer entity, ContactRequest request)
    {
        entity.CompanyName = request.CompanyName.Trim();
        entity.ContactName = request.ContactName?.Trim();
        entity.ContactTitle = request.ContactTitle?.Trim();
        entity.Address = request.Address?.Trim();
        entity.City = request.City?.Trim();
        entity.Region = request.Region?.Trim();
        entity.PostalCode = request.PostalCode?.Trim();
        entity.Country = request.Country?.Trim();
        entity.Phone = request.Phone?.Trim();
        entity.Fax = request.Fax?.Trim();
    }
}

