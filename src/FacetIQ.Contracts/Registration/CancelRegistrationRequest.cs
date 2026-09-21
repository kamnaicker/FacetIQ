using System.ComponentModel.DataAnnotations;

namespace FacetIQ.Contracts.Registration;

/// <summary>The cancellation token from the registration email.</summary>
public sealed record CancelRegistrationRequest
{
    [Required]
    [StringLength(64)]
    public required string Token { get; init; }
}
