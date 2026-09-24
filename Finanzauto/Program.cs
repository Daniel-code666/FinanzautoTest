using Finanzauto.Infrastructure;
using Finanzauto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Finanzauto.Application.Identity;
using Finanzauto.Authentication;
using Finanzauto.Errors;
using Finanzauto.Documentation;
using Finanzauto.Application.Catalog;
using Finanzauto.Application.Partners;
using Finanzauto.Application.Shippers;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.SuppressMapClientErrors = true;
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .SelectMany(entry => entry.Value!.Errors.Select(error =>
                $"{entry.Key}: {(string.IsNullOrWhiteSpace(error.ErrorMessage) ? "El valor enviado no es válido." : error.ErrorMessage)}"));
        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(
            new ApiErrorResponse
            {
                Description = string.Join(" ", errors),
                Exception = "ValidationException",
                HttpCode = StatusCodes.Status400BadRequest
            });
    };
});
builder.Services.AddApiDocumentation();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<IUserAdministrationService, UserAdministrationService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IPartnerService, PartnerService>();
builder.Services.AddScoped<IShipperService, ShipperService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddSingleton<IRandomProductGenerator, RandomProductGenerator>();
builder.Services.AddScoped<IProductGenerationService, ProductGenerationService>();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddConcurrencyLimiter("ProductGeneration", limiter =>
    {
        limiter.PermitLimit = 2;
        limiter.QueueLimit = 0;
    });
    options.AddPolicy("PublicIdentity", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("Finanzauto") ?? throw new InvalidOperationException("Configure ConnectionStrings__Finanzauto."));

var app = builder.Build();

// Opt-in para Compose local. Producción debe aplicar migraciones por separado.
if (app.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<FinanzautoDbContext>().Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

await BootstrapAdmin.InitializeAsync(app.Services, app.Configuration);

app.UseMiddleware<ExceptionHandlingMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger";
        options.SwaggerEndpoint("/openapi/v1.json", "Finanzauto API v1");
        options.DocumentTitle = "Finanzauto API";
    });
}
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", async (FinanzautoDbContext db, CancellationToken cancellationToken) =>
    await db.Database.CanConnectAsync(cancellationToken) ? Results.Ok(new { status = "Healthy" }) : Results.StatusCode(StatusCodes.Status503ServiceUnavailable)).AllowAnonymous();

app.MapControllers();
app.Run();

// Entry point exposed for WebApplicationFactory integration tests.
public partial class Program { }
