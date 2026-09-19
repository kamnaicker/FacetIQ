namespace FacetIQ.Contracts.Norms;

/// <summary>409 body: the rules in force the proposed norm would tie with.</summary>
public sealed record NormConflictResponse
{
    public required IReadOnlyList<NormCollision> Collisions { get; init; }
}

public sealed record NormCollision
{
    public required NormResponse Existing { get; init; }

    public required int Specificity { get; init; }

    /// <summary>Relationship a colliding request would hold. Null when neither rule binds one.</summary>
    public string? OverlappingRelationship { get; init; }

    /// <summary>Purpose a colliding request would state. Null when neither rule binds one.</summary>
    public string? OverlappingPurpose { get; init; }
}
