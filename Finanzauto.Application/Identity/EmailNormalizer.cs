using Finanzauto.Domain.Entities;

namespace Finanzauto.Application.Identity;

public static class EmailNormalizer
{
    public static string Normalize(string email)
    {
        return email.Trim().ToUpperInvariant();
    }

    public static void SetEmail(Employee employee, string email)
    {
        employee.Email = email.Trim();
        employee.NormalizedEmail = Normalize(email);
    }
}
