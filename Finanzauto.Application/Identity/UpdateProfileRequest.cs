using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Finanzauto.Application.Identity;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdateProfileRequest : IValidatableObject
{
    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;
    public DateOnly? BirthDate { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? Region { get; set; }

    [StringLength(20)]
    public string? PostalCode { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }

    [StringLength(30)]
    public string? HomePhone { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (BirthDate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            yield return new ValidationResult("La fecha de nacimiento no puede estar en el futuro.", [nameof(BirthDate)]);
        }
    }
}
