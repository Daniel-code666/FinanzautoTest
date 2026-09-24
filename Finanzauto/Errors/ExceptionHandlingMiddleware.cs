using Finanzauto.Application.Common.Exceptions;
using Microsoft.AspNetCore.WebUtilities;

namespace Finanzauto.Errors;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);

            // Autenticación, autorización, límites y rutas inexistentes no lanzan excepciones.
            // Completar solo errores sin cuerpo, conservando headers como WWW-Authenticate y Allow.
            if (!context.Response.HasStarted && context.Response.StatusCode >= 400 &&
                context.Response.ContentLength is null or 0 &&
                string.IsNullOrEmpty(context.Response.ContentType))
            {
                var code = context.Response.StatusCode;
                await WriteAsync(context, new ApiErrorResponse
                {
                    Description = ReasonPhrases.GetReasonPhrase(code),
                    Exception = "HttpError",
                    HttpCode = code
                });
            }
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // El cliente cerró la conexión: no intentar escribir una respuesta ni convertirla en 500.
            logger.LogDebug("Petición cancelada por el cliente. TraceId: {TraceId}", context.TraceIdentifier);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            var code = exception switch
            {
                ApiException controlled => controlled.StatusCode,
                BadHttpRequestException badRequest => badRequest.StatusCode,
                _ => StatusCodes.Status500InternalServerError
            };

            if (code >= 500)
                logger.LogError(exception, "Error HTTP {HttpCode}. TraceId: {TraceId}", code, context.TraceIdentifier);
            else
                logger.LogWarning(exception, "Error HTTP {HttpCode}. TraceId: {TraceId}", code, context.TraceIdentifier);

            var description = exception is ApiException or BadHttpRequestException || environment.IsDevelopment() ? exception.Message
                : "Ocurrió un error interno al procesar la solicitud.";

            context.Response.Clear();

            await WriteAsync(context, new ApiErrorResponse
            {
                Description = description,
                Exception = exception.GetType().Name,
                HttpCode = code
            });
        }
    }

    private static Task WriteAsync(HttpContext context, ApiErrorResponse error)
    {
        context.Response.StatusCode = error.HttpCode;
        context.Response.ContentLength = null;
        context.Response.Headers.CacheControl = "no-store";
        return context.Response.WriteAsJsonAsync(error, cancellationToken: context.RequestAborted);
    }
}
