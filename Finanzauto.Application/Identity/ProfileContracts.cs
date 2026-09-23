using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Finanzauto.Application.Identity;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdateProfileRequest : IValidatableObject
{
    [Required, StringLength(100)] public string FirstName { get; set; } = string.Empty;
    [Required, StringLength(100)] public string LastName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = string.Empty;
    public DateOnly? BirthDate { get; set; }
    [StringLength(300)] public string? Address { get; set; }
    [StringLength(100)] public string? City { get; set; }
    [StringLength(100)] public string? Region { get; set; }
    [StringLength(20)] public string? PostalCode { get; set; }
    [StringLength(100)] public string? Country { get; set; }
    [StringLength(30)] public string? HomePhone { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (BirthDate > DateOnly.FromDateTime(DateTime.UtcNow))
            yield return new("La fecha de nacimiento no puede estar en el futuro.", [nameof(BirthDate)]);
    }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ChangeProfilePasswordRequest
{
    [Required, StringLength(128)] public string CurrentPassword { get; set; } = string.Empty;
    [Required, StringLength(128, MinimumLength = 12)] public string NewPassword { get; set; } = string.Empty;
}

public sealed record ProfileResponse(
    int Id, string FirstName, string LastName, string Email, DateOnly? BirthDate,
    string? Address, string? City, string? Region, string? PostalCode, string? Country,
    string? HomePhone, int RoleId, string RoleName, DateTime CreationDate, DateTime? UpdatedDate);

public interface IProfileService
{
    Task<ProfileResponse> GetAsync(int userId, CancellationToken ct);
    Task<ProfileResponse> UpdateAsync(int userId, UpdateProfileRequest request, CancellationToken ct);
    Task ChangePasswordAsync(int userId, ChangeProfilePasswordRequest request, CancellationToken ct);
}
