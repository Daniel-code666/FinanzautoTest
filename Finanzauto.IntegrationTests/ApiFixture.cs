using System.Net.Http.Headers;
using System.Net.Http.Json;
using Finanzauto.Application.Identity;
using Finanzauto.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Finanzauto.IntegrationTests;

public sealed class PostgresFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder("postgres:17").Build();
    public Task InitializeAsync() => Container.StartAsync();
    public Task DisposeAsync() => Container.DisposeAsync().AsTask();
}

[CollectionDefinition("PostgreSQL")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture> { }

public sealed class ApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    public string AdminEmail { get; } = "admin@example.test";
    public string Password { get; } = Guid.NewGuid().ToString("N");
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("ConnectionStrings:Finanzauto", connectionString);
        builder.UseSetting("Database:ApplyMigrations", "true");
        builder.UseSetting("BootstrapAdmin:Email", AdminEmail);
        builder.UseSetting("BootstrapAdmin:Password", Password);
        builder.UseSetting("Jwt:SigningKey", Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"));
        builder.UseSetting("Jwt:Issuer", "Finanzauto.Tests");
        builder.UseSetting("Jwt:Audience", "Finanzauto.Tests");
        builder.UseSetting("Logging:LogLevel:Default", "Warning");
    }
}

public abstract class ApiTest(PostgresFixture postgres) : IAsyncLifetime
{
    private readonly string database = "test_" + Guid.NewGuid().ToString("N");
    private string connectionString = "";
    protected ApiFactory Factory { get; private set; } = null!;
    protected HttpClient Anonymous { get; private set; } = null!;
    protected HttpClient Admin { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await using var connection = new NpgsqlConnection(postgres.Container.GetConnectionString());
        await connection.OpenAsync();
        // Name is generated internally from a GUID, never from external input.
        await using var command = new NpgsqlCommand($"CREATE DATABASE {database}", connection);
        await command.ExecuteNonQueryAsync();
        connectionString = new NpgsqlConnectionStringBuilder(postgres.Container.GetConnectionString()) { Database = database }.ConnectionString;
        Factory = new ApiFactory(connectionString);
        Anonymous = Factory.CreateClient();
        Anonymous.Timeout = TimeSpan.FromMinutes(3);
        Admin = await Login(Factory.AdminEmail, Factory.Password);
    }

    protected FinanzautoDbContext Db() => new(new DbContextOptionsBuilder<FinanzautoDbContext>()
        .UseNpgsql(connectionString).Options);

    protected async Task<HttpClient> Login(string email, string password)
    {
        using var response = await Anonymous.PostAsJsonAsync("/Login", new { email, password });
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        var client = Factory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(3);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
        return client;
    }

    protected async Task<(HttpClient Client, UserResponse User)> Register()
    {
        var email = Guid.NewGuid().ToString("N") + "@example.test";
        using var response = await Anonymous.PostAsJsonAsync("/UserAdministration/Register",
            new { firstName = "Test", lastName = "User", email, password = Factory.Password });
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var user = (await response.Content.ReadFromJsonAsync<UserResponse>())!;
        Assert.Equal("User", user.RoleName);
        return (await Login(email, Factory.Password), user);
    }

    public async Task DisposeAsync()
    {
        Admin?.Dispose();
        Anonymous?.Dispose();
        try
        {
            if (Factory is not null) await Factory.DisposeAsync();
        }
        finally
        {
            await using var connection = new NpgsqlConnection(postgres.Container.GetConnectionString());
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS {database} WITH (FORCE)", connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}

