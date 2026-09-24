using Finanzauto.Application.Orders;
using Finanzauto.Application.Shippers;
using Finanzauto.Infrastructure.Shippers;
using Finanzauto.Infrastructure.Orders;
using Finanzauto.Infrastructure.Persistence;
using Finanzauto.Application.Identity;
using Finanzauto.Domain.Entities;
using Finanzauto.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Finanzauto.Application.Catalog;
using Finanzauto.Infrastructure.Catalog;
using Finanzauto.Application.Partners;
using Finanzauto.Infrastructure.Partners;

namespace Finanzauto.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<FinanzautoDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IIdentityStore, IdentityStore>();
        services.AddScoped<IPasswordHasher<Employee>, PasswordHasher<Employee>>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<ICatalogStore, CatalogStore>();
        services.AddScoped<IBulkProductWriter, BulkProductWriter>();
        services.AddScoped<IPartnerStore, PartnerStore>();
        services.AddScoped<IOrderStore, OrderStore>();
        services.AddScoped<IShipperStore, ShipperStore>();
        return services;
    }
}
