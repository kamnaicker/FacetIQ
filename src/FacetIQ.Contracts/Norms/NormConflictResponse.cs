namespace FacetIQ.Contracts.Norms;

/// <summary>
/// Why a norm was not stored. Each collision names a rule already in force that the proposed one
/// could not be told apart from.
///
/// The subject resolves this, never the engine. Contextual integrity gives no flow parameter
/// precedence over another, so there is no principled basis on which to break the tie on their
/// behalf -- the same reasoning the ranker applies when a tie reaches it at request time.
/// </summary>
public sealed record NormConflictResponse
{
    public required IReadOnlyList<NormCollision> Collisions { get; init; }
}

/// <summary>One existing rule the proposed norm collides with, and the shape of the collision.</summary>
public sealed record NormCollision
{
    public required NormResponse Existing { get; init; }

    /// <summary>
    /// The score the two share. Equal specificity is what makes this a tie rather than one norm
    /// simply governing the other.
    /// </summary>
    public required int Specificity { get; init; }

    /// <summary>
    /// The conditions a request would have to carry to match both rules at once. A null here is
    /// a condition neither rule binds, so any value collides.
    /// </summary>
    public string? OverlappingRelationship { get; init; }

    public string? OverlappingPurpose { get; init; }
}
