using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Finanzauto.Infrastructure.Persistence;

public sealed class FinanzautoDbContextFactory : IDesignTimeDbContextFactory<FinanzautoDbContext>
{
    public FinanzautoDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Finanzauto") ?? "Host=localhost;Port=5432;Database=finanzauto;Username=finanzauto";
        return new FinanzautoDbContext(new DbContextOptionsBuilder<FinanzautoDbContext>()
            .UseNpgsql(connectionString).Options);
    }
}
