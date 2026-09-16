using System.ComponentModel.DataAnnotations;

namespace FacetIQ.Contracts.Disclosure;

/// <summary>
/// The requester comes from the token and relationships from accepted standings, so neither is
/// sent. Lengths mirror the columns these values are compared against.
/// </summary>
public sealed record DisclosureRequestDto
{
    /// <summary>Subjects are addressed by email; profile ids are never exposed.</summary>
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public required string SubjectEmail { get; init; }

    [Required]
    [StringLength(64)]
    public required string AttributeKey { get; init; }

    [Required]
    [StringLength(32)]
    public required string Purpose { get; init; }
}
