using System.ComponentModel.DataAnnotations;

namespace FacetIQ.Contracts.Norms;

/// <summary>
/// A rule the subject is authoring. The subject is absent deliberately: it comes from the
/// authenticated principal, so a caller cannot write rules into someone else's profile.
///
/// Omitting a condition is meaningful rather than incomplete -- an unbound condition is a wildcard.
/// Lengths mirror the columns in NormConfiguration.
/// </summary>
public sealed record CreateNormRequest
{
    /// <summary>The claim this norm selects. Selection precedes any transformation.</summary>
    public required Guid AttributeId { get; init; }

    [StringLength(64)]
    public string? Relationship { get; init; }

    [StringLength(32)]
    public string? Purpose { get; init; }

    /// <summary>
    /// Omitted, or None, releases the claim unchanged. The action is derived from this and the
    /// deny reason rather than stated separately, so the two cannot contradict each other.
    /// </summary>
    [StringLength(32)]
    public string? Transform { get; init; }

    [StringLength(64)]
    public string? TransformParameter { get; init; }

    /// <summary>Set to author an explicit refusal in place of a disclosure.</summary>
    [StringLength(32)]
    public string? DenyReason { get; init; }

    [Required]
    [StringLength(256)]
    public required string JustifyingPrinciple { get; init; }
}
