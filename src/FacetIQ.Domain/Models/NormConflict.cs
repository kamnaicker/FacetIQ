using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;

namespace FacetIQ.Domain.Models;

/// <summary>Two norms one request could match at equal specificity, found when authoring.</summary>
public sealed record NormConflict
{
    public required Norm Proposed { get; init; }

    public required Norm Existing { get; init; }

    public required int Specificity { get; init; }

    /// <summary>Relationship a colliding request would hold. Null when neither norm binds one.</summary>
    public string? OverlappingRelationship { get; init; }

    /// <summary>Purpose a colliding request would state. Null when neither norm binds one.</summary>
    public Purpose? OverlappingPurpose { get; init; }

    // Callers have already checked the scores are equal.
    public static NormConflict Between(Norm proposed, Norm existing) => new()
    {
        Proposed = proposed,
        Existing = existing,
        Specificity = NormSpecificity.Of(proposed),
        OverlappingRelationship = proposed.Relationship ?? existing.Relationship,
        OverlappingPurpose = proposed.Purpose ?? existing.Purpose
    };
}
