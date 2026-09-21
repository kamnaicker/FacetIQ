using System.ComponentModel.DataAnnotations;

namespace FacetIQ.Contracts.Registration;

/// <summary>Redeems the code emailed for a pending attempt.</summary>
public sealed record ConfirmRegistrationRequest
{
    [Required]
    public required Guid RegistrationId { get; init; }

    [Required]
    [StringLength(6, MinimumLength = 6)]
    [RegularExpression("^[0-9]{6}$", ErrorMessage = "Must be six digits.")]
    public required string Code { get; init; }

    [Required]
    [StringLength(128)]
    public required string Password { get; init; }
}
