namespace FacetIQ.Contracts.Norms;

public sealed record NormResponse
{
    public required Guid Id { get; init; }

    public required int Version { get; init; }

    public required Guid AttributeId { get; init; }

    /// <summary>Null is a wildcard.</summary>
    public string? Relationship { get; init; }

    /// <summary>Null is a wildcard.</summary>
    public string? Purpose { get; init; }

    public required string Action { get; init; }

    public required string Transform { get; init; }

    public string? TransformParameter { get; init; }

    public string? DenyReason { get; init; }

    public required string JustifyingPrinciple { get; init; }

    /// <summary>Bound conditions, 0 to 2. Higher wins; equal is a tie.</summary>
    public required int Specificity { get; init; }
}
