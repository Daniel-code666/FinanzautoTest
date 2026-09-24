namespace Finanzauto.Errors;

/// <summary>Contrato único de errores HTTP; Exception contiene el nombre del tipo, no el objeto CLR.</summary>
public class ApiErrorResponse
{
    public string Description { get; set; } = string.Empty;
    public string Exception { get; set; } = string.Empty;
    public int HttpCode { get; set; }
}
