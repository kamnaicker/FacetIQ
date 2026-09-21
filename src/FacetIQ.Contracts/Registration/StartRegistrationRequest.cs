using System.ComponentModel.DataAnnotations;

namespace FacetIQ.Contracts.Registration;

/// <summary>
/// Starts a registration. [EmailAddress] is only a coarse filter; RegistrationService parses the
/// address properly.
/// </summary>
public sealed record StartRegistrationRequest
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public required string Email { get; init; }

    [Required]
    [StringLength(128)]
    public required string Password { get; init; }
}
