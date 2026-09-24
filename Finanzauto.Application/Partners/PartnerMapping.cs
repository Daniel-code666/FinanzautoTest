using Finanzauto.Domain.Entities;

namespace Finanzauto.Application.Partners;

public static class PartnerMapping
{
    public static SupplierDetailResponse ToResponse(this Supplier entity)
    {
        return new SupplierDetailResponse
        {
            Id = entity.SupplierId,
            CompanyName = entity.CompanyName,
            ContactName = entity.ContactName,
            ContactTitle = entity.ContactTitle,
            Address = entity.Address,
            City = entity.City,
            Region = entity.Region,
            PostalCode = entity.PostalCode,
            Country = entity.Country,
            Phone = entity.Phone,
            Fax = entity.Fax,
            HomePage = entity.HomePage,
            CreationDate = entity.CreationDate,
            UpdatedDate = entity.UpdatedDate
        };
    }

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
    public static CustomerResponse ToResponse(this Customer entity)
    {
        return new CustomerResponse
        {
            Id = entity.CustomerId,
            CompanyName = entity.CompanyName,
            ContactName = entity.ContactName,
            ContactTitle = entity.ContactTitle,
            Address = entity.Address,
            City = entity.City,
            Region = entity.Region,
            PostalCode = entity.PostalCode,
            Country = entity.Country,
            Phone = entity.Phone,
            Fax = entity.Fax,
            CreationDate = entity.CreationDate,
            UpdatedDate = entity.UpdatedDate
        };
    }

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

