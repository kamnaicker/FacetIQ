namespace FacetIQ.Contracts.Norms;

/// <summary>
/// A norm as its author sees it. Specificity travels with it because a subject deciding whether
/// to add a rule needs to know how it will rank against the ones already written, and that score
/// is not obvious from reading the conditions back.
/// </summary>
public sealed record NormResponse
{
    public required Guid Id { get; init; }

    /// <summary>Identity and revision together, since editing a norm writes a new row.</summary>
    public required int Version { get; init; }

    public required Guid AttributeId { get; init; }

    /// <summary>Null is a wildcard rather than a missing value.</summary>
    public string? Relationship { get; init; }

    public string? Purpose { get; init; }

    public required string Action { get; init; }

    public required string Transform { get; init; }

    public string? TransformParameter { get; init; }

    public string? DenyReason { get; init; }

    public required string JustifyingPrinciple { get; init; }

    /// <summary>How many conditions this norm binds. Higher governs lower; equal is a tie.</summary>
    public required int Specificity { get; init; }
}
