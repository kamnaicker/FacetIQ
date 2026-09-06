using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;
using FacetIQ.Domain.Models;
using FacetIQ.Services.Authoring;

namespace FacetIQ.Services.Tests;

/// <summary>
/// The authoring half of the ambiguity story. The engine's matching tests assert what happens to
/// a tie that already exists; these assert that the subject is told about it while they are still
/// writing the rule, which is the only point at which they can do anything about it.
/// </summary>
public class ConflictDetectorTests
{
    private static readonly Guid SubjectId = new("0a5f4d8e-0000-4000-8000-000000000001");

    private static readonly Guid LegalName = new("0a5f4d8e-0000-4000-8000-000000000010");
    private static readonly Guid SocialName = new("0a5f4d8e-0000-4000-8000-000000000012");

    /// <summary>
    /// O8: the pair from <c>EquallySpecificNorms_AreRefusedAsAmbiguous</c>, caught one step
    /// earlier. A social request from a friend satisfies both, neither is more specific, and they
    /// select different claims.
    ///
    /// Both norms return, so an implementation comparing only the action would call this agreement
    /// and store the norm. What they disagree about is which name is released, which is the whole
    /// subject of the project.
    /// </summary>
    [Fact]
    public void EquallySpecificNorms_ThatSelectDifferentClaims_AreRefusedWhenAuthored()
    {
        var existing = Rule(LegalName, purpose: Purpose.Social);
        var proposed = Rule(SocialName, relationship: "friend");

        var conflict = Assert.Single(Detect(proposed, existing));

        Assert.Same(proposed, conflict.Proposed);
        Assert.Same(existing, conflict.Existing);
        Assert.Equal(1, conflict.Specificity);

        // The request that witnesses the collision: each norm binds the condition the other
        // leaves open, so the overlap carries both.
        Assert.Equal("friend", conflict.OverlappingRelationship);
        Assert.Equal(Purpose.Social, conflict.OverlappingPurpose);
    }

    /// <summary>
    /// O8: the control. Two norms of equal specificity whose conditions cannot both be satisfied
    /// never compete, so equal scores alone are not a conflict. Without this, a detector that
    /// refused every tie in score would pass the test above and still be wrong.
    /// </summary>
    [Fact]
    public void EquallySpecificNorms_WithDisjointConditions_AreAccepted()
    {
        var existing = Rule(LegalName, purpose: Purpose.Regulatory);
        var proposed = Rule(SocialName, purpose: Purpose.Social);

        Assert.Empty(Detect(proposed, existing));
    }

    /// <summary>
    /// O8: the pair from the first test again, with the existing norm superseded and nothing else
    /// changed. A norm is never edited in place -- a change writes a new revision and retires the
    /// old one -- so without this, revising a rule would collide with the very rule it replaces
    /// and a subject could never edit anything.
    ///
    /// It follows from what supersession already means: a retired norm can no longer govern a
    /// request, so there is no request it could collide over.
    /// </summary>
    [Fact]
    public void SupersededNorm_CannotCollide_SoAnEditDoesNotConflict()
    {
        var retired = Rule(LegalName, purpose: Purpose.Social, supersededAt: DateTimeOffset.UnixEpoch);
        var proposed = Rule(SocialName, relationship: "friend");

        Assert.Empty(Detect(proposed, retired));
    }

    private static IReadOnlyList<NormConflict> Detect(Norm proposed, params Norm[] existing) =>
        new ConflictDetector().Detect(proposed, existing);

    private static Norm Rule(
        Guid attributeId,
        Purpose? purpose = null,
        string? relationship = null,
        TransformKind transform = TransformKind.None,
        string? parameter = null,
        DateTimeOffset? supersededAt = null) => new()
    {
        Id = Guid.NewGuid(),
        Version = 1,
        SubjectId = SubjectId,
        AttributeId = attributeId,
        Purpose = purpose,
        Relationship = relationship,
        Action = transform == TransformKind.None ? ActionType.Return : ActionType.Transform,
        Transform = transform,
        TransformParameter = parameter,
        SupersededAt = supersededAt,
        JustifyingPrinciple = "Test rule."
    };
}
