using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;

namespace FacetIQ.Domain.Models;

/// <summary>
/// Two norms one request could match at equal specificity while disagreeing about the outcome.
/// Reported when a norm is authored, so the subject resolves the tie rather than meeting it later.
/// </summary>
public sealed record NormConflict
{
    /// <summary>Not persisted while a conflict stands against it.</summary>
    public required Norm Proposed { get; init; }

    public required Norm Existing { get; init; }

    /// <summary>The score the two share, which is what makes this a tie rather than one governing.</summary>
    public required int Specificity { get; init; }

    /// <summary>
    /// The conditions a request would need to match both. Null is a condition neither norm binds,
    /// so any value collides.
    /// </summary>
    public string? OverlappingRelationship { get; init; }

    public Purpose? OverlappingPurpose { get; init; }

    /// <summary>
    /// Derives the score and the overlap rather than letting a caller state them, as
    /// <see cref="DisclosureResult.Disclosed"/> derives its outcome. The two scores are equal by
    /// the time a conflict exists, so either serves.
    /// </summary>
    public static NormConflict Between(Norm proposed, Norm existing) => new()
    {
        Proposed = proposed,
        Existing = existing,
        Specificity = NormSpecificity.Of(proposed),
        OverlappingRelationship = proposed.Relationship ?? existing.Relationship,
        OverlappingPurpose = proposed.Purpose ?? existing.Purpose
    };
}
