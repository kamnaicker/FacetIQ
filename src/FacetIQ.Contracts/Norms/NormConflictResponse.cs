namespace FacetIQ.Contracts.Norms;

/// <summary>
/// Why a norm was not stored. The subject resolves this, never the engine: contextual integrity
/// gives no flow parameter precedence, so there is no basis for breaking the tie on their behalf.
/// </summary>
public sealed record NormConflictResponse
{
    public required IReadOnlyList<NormCollision> Collisions { get; init; }
}

/// <summary>One existing rule the proposed norm collides with, and the shape of the collision.</summary>
public sealed record NormCollision
{
    public required NormResponse Existing { get; init; }

    /// <summary>The score the two share, which is what makes this a tie rather than one governing.</summary>
    public required int Specificity { get; init; }

    /// <summary>
    /// The conditions a request would need to match both. Null is a condition neither rule binds,
    /// so any value collides.
    /// </summary>
    public string? OverlappingRelationship { get; init; }

    public string? OverlappingPurpose { get; init; }
}
