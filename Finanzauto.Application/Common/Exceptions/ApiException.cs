namespace Finanzauto.Application.Common.Exceptions;

/// <summary>Excepción controlada cuyo mensaje se puede mostrar al cliente.</summary>
public class ApiException : Exception
{
    public int StatusCode { get; }

    public ApiException(int statusCode, string message) : base(message)
    {
        if (statusCode < 400 || statusCode > 599)
        {
            throw new ArgumentOutOfRangeException(nameof(statusCode), "El código debe estar entre 400 y 599.");
        }

        StatusCode = statusCode;
    }
}
