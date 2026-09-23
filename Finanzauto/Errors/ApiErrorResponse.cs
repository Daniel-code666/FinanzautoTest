namespace Finanzauto.Errors;

/// <summary>Contrato único de errores HTTP; Exception contiene el nombre del tipo, no el objeto CLR.</summary>
public sealed record ApiErrorResponse(string Description, string Exception, int HttpCode);
