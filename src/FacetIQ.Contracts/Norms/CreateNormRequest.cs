using System.ComponentModel.DataAnnotations;

namespace FacetIQ.Contracts.Norms;

/// <summary>
/// The subject comes from the token. An omitted condition is a wildcard. Lengths mirror
/// NormConfiguration.
/// </summary>
public sealed record CreateNormRequest
{
    public required Guid AttributeId { get; init; }

    [StringLength(64)]
    public string? Relationship { get; init; }

    [StringLength(32)]
    public string? Purpose { get; init; }

    /// <summary>Omitted or None releases the claim unchanged.</summary>
    [StringLength(32)]
    public string? Transform { get; init; }

    /// <summary>Generalise: the age threshold, default 18.</summary>
    [StringLength(64)]
    public string? TransformParameter { get; init; }

    /// <summary>Set to RefusedByRule to make this a refusal. No other value is accepted.</summary>
    [StringLength(32)]
    public string? DenyReason { get; init; }

    [Required]
    [StringLength(256)]
    public required string JustifyingPrinciple { get; init; }
}
