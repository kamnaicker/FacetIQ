using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;

namespace FacetIQ.Domain.Models;

/// <summary>
/// Two norms that one request could match at equal specificity while disagreeing about the
/// outcome. Reported when a norm is authored rather than when a request arrives, so the subject
/// resolves the tie themselves instead of meeting an ambiguity refusal later, once they have
/// forgotten what they wrote.
///
/// The conflict names both norms rather than only the existing one, so it stands on its own
/// once it has left the detector and reached a response or a screen.
/// </summary>
public sealed record NormConflict
{
    /// <summary>The norm being authored. It is not persisted while a conflict stands against it.</summary>
    public required Norm Proposed { get; init; }

    /// <summary>The norm already in force that it collides with.</summary>
    public required Norm Existing { get; init; }

    /// <summary>
    /// The score the two share. Equal specificity is what makes this a tie rather than one norm
    /// simply governing the other, and it is read from the same definition matching uses.
    /// </summary>
    public required int Specificity { get; init; }

    /// <summary>
    /// The conditions a request would have to carry to match both norms -- the request that
    /// witnesses the collision. Null is a condition neither norm binds, so any value collides.
    /// </summary>
    public string? OverlappingRelationship { get; init; }

    public Purpose? OverlappingPurpose { get; init; }

    /// <summary>
    /// Derives the shared score and the overlap rather than letting a caller state them, on the
    /// same reasoning as <see cref="DisclosureResult.Disclosed"/>: they are facts about the pair,
    /// not choices. The two scores are equal by the time a conflict exists, so either serves.
    ///
    /// A null condition is a wildcard admitting anything, so the overlap takes whichever norm
    /// binds one. Where both bind, they are equal, or the pair could not have collided.
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
