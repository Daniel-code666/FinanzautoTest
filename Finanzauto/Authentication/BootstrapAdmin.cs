using System.ComponentModel.DataAnnotations;
using Finanzauto.Application.Identity;

namespace Finanzauto.Authentication;

public static class BootstrapAdmin
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        var email = configuration["BootstrapAdmin:Email"];
        var password = configuration["BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password)) return;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Configure tanto BootstrapAdmin:Email como BootstrapAdmin:Password.");

        await using var scope = services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IIdentityStore>();
        var existing = await store.FindByEmailAsync(EmailNormalizer.Normalize(email), CancellationToken.None);
        if (existing is not null)
        {
            if (existing.RoleId != UserAdministrationService.AdminRoleId)
                throw new InvalidOperationException("El correo del administrador inicial ya pertenece a otro usuario.");
            return; // No sobrescribir contraseñas, roles ni el estado de una cuenta existente.
        }
        var request = new CreateUserRequest
        {
            Email = email,
            Password = password,
            FirstName = "Administrador",
            LastName = "Inicial",
            RoleId = UserAdministrationService.AdminRoleId
        };
        Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
        await scope.ServiceProvider.GetRequiredService<IUserAdministrationService>()
            .CreateAsync(request, CancellationToken.None);
    }
}

