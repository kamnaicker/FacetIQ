using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;
using FacetIQ.Domain.Models;
using FacetIQ.Services.Authoring;

namespace FacetIQ.Services.Tests;

/// <summary>
/// The authoring half of the ambiguity story. The engine's tests assert what happens to a tie
/// that already exists; these assert the subject is told while they can still act on it.
/// </summary>
public class ConflictDetectorTests
{
    private static readonly Guid SubjectId = new("0a5f4d8e-0000-4000-8000-000000000001");

    private static readonly Guid LegalName = new("0a5f4d8e-0000-4000-8000-000000000010");
    private static readonly Guid SocialName = new("0a5f4d8e-0000-4000-8000-000000000012");

    /// <summary>
    /// O8: the pair from <c>EquallySpecificNorms_AreRefusedAsAmbiguous</c>, caught one step
    /// earlier. Both norms return, so comparing only the action would call this agreement and
    /// store it. What they disagree about is which name is released.
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
    /// O8: the control. Equal scores alone are not a conflict. Without this, a detector refusing
    /// every tie in score would pass the test above and still be wrong.
    /// </summary>
    [Fact]
    public void EquallySpecificNorms_WithDisjointConditions_AreAccepted()
    {
        var existing = Rule(LegalName, purpose: Purpose.Regulatory);
        var proposed = Rule(SocialName, purpose: Purpose.Social);

        Assert.Empty(Detect(proposed, existing));
    }

    /// <summary>
    /// O8: the first test's pair with the existing norm superseded and nothing else changed. A
    /// norm is never edited in place, so without this a subject could never revise a rule -- the
    /// revision would collide with what it replaces. A retired norm governs no request, so there
    /// is none for it to collide over.
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
