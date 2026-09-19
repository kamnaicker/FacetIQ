using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;
using FacetIQ.Domain.Models;
using FacetIQ.Services.Authoring;

namespace FacetIQ.Services.Tests.Authoring;

public class ConflictDetectorTests
{
    private static readonly Guid SubjectId = new("0a5f4d8e-0000-4000-8000-000000000001");

    private static readonly Guid LegalName = new("0a5f4d8e-0000-4000-8000-000000000010");
    private static readonly Guid SocialName = new("0a5f4d8e-0000-4000-8000-000000000012");

    /// <summary>O8: a social request from a friend matches both at specificity 1.</summary>
    [Fact]
    public void EquallySpecificNorms_ThatSelectDifferentClaims_AreRefusedWhenAuthored()
    {
        var existing = Rule(LegalName, purpose: Purpose.Social);
        var proposed = Rule(SocialName, relationship: "friend");

        var conflict = Assert.Single(Detect(proposed, existing));

        Assert.Same(proposed, conflict.Proposed);
        Assert.Same(existing, conflict.Existing);
        Assert.Equal(1, conflict.Specificity);
        Assert.Equal("friend", conflict.OverlappingRelationship);
        Assert.Equal(Purpose.Social, conflict.OverlappingPurpose);
    }

    /// <summary>O8: the ranker refuses any tie, so a rule releasing the same thing still collides.</summary>
    [Fact]
    public void EquallySpecificNorms_ThatReleaseTheSameClaim_AreRefusedWhenAuthored()
    {
        var existing = Rule(SocialName, purpose: Purpose.Social);
        var proposed = Rule(SocialName, purpose: Purpose.Social);

        Assert.Single(Detect(proposed, existing));
    }

    /// <summary>O8: equal scores alone are not a conflict.</summary>
    [Fact]
    public void EquallySpecificNorms_WithDisjointConditions_AreAccepted()
    {
        var existing = Rule(LegalName, purpose: Purpose.Regulatory);
        var proposed = Rule(SocialName, purpose: Purpose.Social);

        Assert.Empty(Detect(proposed, existing));
    }

    /// <summary>
    /// A requester holding both standings is refused as ambiguous at request time. Flagging the pair
    /// here would stop a subject writing one rule per relationship.
    /// </summary>
    [Fact]
    public void NormsBoundToDifferentRelationships_AreAccepted()
    {
        var existing = Rule(LegalName, relationship: "colleague");
        var proposed = Rule(SocialName, relationship: "friend");

        Assert.Empty(Detect(proposed, existing));
    }

    [Fact]
    public void RetiredNorm_CannotCollide()
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
        DateTimeOffset? supersededAt = null) => new()
    {
        Id = Guid.NewGuid(),
        Version = 1,
        SubjectId = SubjectId,
        AttributeId = attributeId,
        Purpose = purpose,
        Relationship = relationship,
        Action = ActionType.Return,
        Transform = TransformKind.None,
        SupersededAt = supersededAt,
        JustifyingPrinciple = "Test rule."
    };
}
