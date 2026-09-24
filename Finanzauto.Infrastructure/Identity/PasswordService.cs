using Finanzauto.Application.Identity;
using Finanzauto.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Finanzauto.Infrastructure.Identity;

public sealed class PasswordService(IPasswordHasher<Employee> hasher) : IPasswordService
{
    public string Hash(Employee employee, string password) => hasher.HashPassword(employee, password);

    public bool Verify(Employee employee, string password)
    {
        try
        {
            return hasher.VerifyHashedPassword(employee, employee.PasswordHash, password) != PasswordVerificationResult.Failed;
        }
        catch (FormatException) { return false; }
    }
}

