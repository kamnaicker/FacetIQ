using System.ComponentModel.DataAnnotations;

namespace FacetIQ.Contracts.Registration;

/// <summary>Asks for a fresh code on a pending attempt.</summary>
public sealed record ResendRegistrationRequest
{
    [Required]
    public required Guid RegistrationId { get; init; }
}
