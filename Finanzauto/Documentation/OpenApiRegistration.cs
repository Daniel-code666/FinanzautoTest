using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;

namespace Finanzauto.Documentation;

public static class OpenApiRegistration
{
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info.Title = "Finanzauto API";
                document.Info.Version = "v1";
                document.Info.Description = "API de productos y administración de usuarios. Inicia sesión en POST /Login y pega accessToken en Authorize.";
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Pega únicamente el accessToken, sin el prefijo Bearer."
                };
                document.Security =
                [
                    new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                    }
                ];
                return Task.CompletedTask;
            });
            options.AddOperationTransformer((operation, context, cancellationToken) =>
            {
                if (context.Description.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
                    operation.Security = [];
                return Task.CompletedTask;
            });
        });
        return services;
    }
}
