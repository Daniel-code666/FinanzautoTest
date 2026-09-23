using Finanzauto.Application.Common.Exceptions;

namespace Finanzauto.Application.Identity;

public sealed class IdentityException(int statusCode, string message) : ApiException(statusCode, message);
