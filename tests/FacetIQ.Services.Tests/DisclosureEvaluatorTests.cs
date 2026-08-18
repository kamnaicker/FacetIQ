using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;
using FacetIQ.Domain.Models;
using FacetIQ.Services.Auditing;
using FacetIQ.Services.Disclosure;
using FacetIQ.Services.Matching;
using FacetIQ.Services.Transformation;

namespace FacetIQ.Services.Tests;

/// <summary>
/// Conformity tests. Each asserts one project objective against the decision the engine
/// reaches and the record it leaves behind.
/// </summary>
public class DisclosureEvaluatorTests
{
    private static readonly Guid SubjectId = new("0a5f4d8e-0000-4000-8000-000000000001");

    private static readonly SubjectAttribute LegalName = Claim("name", "Amara Chidinma Nwosu", "legal");
    private static readonly SubjectAttribute ProfessionalName = Claim("name", "Dr Amara Nwosu", "professional");
    private static readonly SubjectAttribute SocialName = Claim("name", "Amara", "social");
    private static readonly SubjectAttribute DateOfBirth = Claim("dateOfBirth", "1994-03-11", "legal");

    /// <summary>
    /// O9: the same stored claim yields different representations across different contexts.
    /// This is the project's central assertion, and the three names are irreducible -- none
    /// can be derived from another, so selection cannot be replaced by transformation.
    /// </summary>
    [Fact]
    public async Task SameClaim_AcrossThreeContexts_YieldsThreeRepresentations()
    {
        var norms = new[]
        {
            Rule(LegalName, purpose: Purpose.Regulatory),
            Rule(ProfessionalName, purpose: Purpose.Clinical, relationship: "colleague"),
            Rule(SocialName, purpose: Purpose.Social)
        };

        var regulatory = await Evaluate(norms, Ask("name", Purpose.Regulatory));
        var clinical = await Evaluate(norms, Ask("name", Purpose.Clinical, "colleague"));
        var social = await Evaluate(norms, Ask("name", Purpose.Social));

        Assert.Equal("Amara Chidinma Nwosu", regulatory.Value);
        Assert.Equal("Dr Amara Nwosu", clinical.Value);
        Assert.Equal("Amara", social.Value);
    }

    /// <summary>O8: equally specific norms that both apply are a tie the engine refuses to break.</summary>
    [Fact]
    public async Task EquallySpecificNorms_AreRefusedAsAmbiguous()
    {
        var norms = new[]
        {
            Rule(LegalName, purpose: Purpose.Social),
            Rule(SocialName, relationship: "friend")
        };

        var result = await Evaluate(norms, Ask("name", Purpose.Social, "friend"));

        Assert.Equal(ActionType.Deny, result.Outcome);
        Assert.Equal(DenyReasonCode.AmbiguousNorms, result.DenyReason);
    }

    /// <summary>
    /// The control for the test above: two norms of equal specificity whose conditions cannot
    /// both be satisfied never compete, so equal scores alone do not constitute a conflict.
    /// </summary>
    [Fact]
    public async Task EquallySpecificNorms_WithDisjointConditions_DoNotCompete()
    {
        var norms = new[]
        {
            Rule(LegalName, purpose: Purpose.Regulatory),
            Rule(SocialName, purpose: Purpose.Social)
        };

        var result = await Evaluate(norms, Ask("name", Purpose.Social));

        Assert.Equal(ActionType.Return, result.Outcome);
        Assert.Equal("Amara", result.Value);
    }

    /// <summary>A norm binding more conditions governs over one that binds fewer.</summary>
    [Fact]
    public async Task MoreSpecificNorm_GovernsOverWildcard()
    {
        var norms = new[]
        {
            Rule(SocialName),
            Rule(ProfessionalName, purpose: Purpose.Clinical, relationship: "colleague")
        };

        var result = await Evaluate(norms, Ask("name", Purpose.Clinical, "colleague"));

        Assert.Equal("Dr Amara Nwosu", result.Value);
    }

    /// <summary>O7: absence of an applicable norm is a refusal, never a best guess.</summary>
    [Fact]
    public async Task NoApplicableNorm_IsRefused()
    {
        var norms = new[] { Rule(LegalName, purpose: Purpose.Regulatory) };

        var result = await Evaluate(norms, Ask("name", Purpose.Social));

        Assert.Equal(ActionType.Deny, result.Outcome);
        Assert.Equal(DenyReasonCode.NoMatchingNorm, result.DenyReason);
        Assert.Null(result.Value);
    }

    /// <summary>
    /// O4: a transform returns a value that is coarser than the stored one and still true of
    /// the subject. The released value was never stored, which is the step omission alone
    /// cannot reach.
    /// </summary>
    [Fact]
    public async Task Transform_ReturnsCoarserValueThanStored()
    {
        var norms = new[]
        {
            Rule(DateOfBirth, purpose: Purpose.Social, transform: TransformKind.Generalise, parameter: "18")
        };

        var result = await Evaluate(norms, Ask("dateOfBirth", Purpose.Social));

        Assert.Equal(ActionType.Transform, result.Outcome);
        Assert.Equal("over 18", result.Value);
        Assert.NotEqual(DateOfBirth.Value, result.Value);
    }

    /// <summary>O5 and O6: every outcome is recorded exactly once, and without the value released.</summary>
    [Fact]
    public async Task EveryOutcome_IsRecordedOnce_WithoutTheDisclosedValue()
    {
        var audit = new RecordingAuditRepository();
        var norms = new[] { Rule(SocialName, purpose: Purpose.Social) };

        var result = await Evaluate(norms, Ask("name", Purpose.Social), audit);

        var record = Assert.Single(audit.Written);
        Assert.Equal(ActionType.Return, record.Outcome);
        Assert.Equal(norms[0].Id, record.NormId);
        Assert.Equal(norms[0].Version, record.NormVersion);
        Assert.DoesNotContain(result.Value!, record.JustifyingPrinciple ?? string.Empty);
    }

    [Fact]
    public async Task RefusedRequest_IsAuditedLikeAnyOther()
    {
        var audit = new RecordingAuditRepository();

        await Evaluate([Rule(LegalName, purpose: Purpose.Regulatory)], Ask("name", Purpose.Social), audit);

        var record = Assert.Single(audit.Written);
        Assert.Equal(ActionType.Deny, record.Outcome);
        Assert.Equal(DenyReasonCode.NoMatchingNorm, record.DenyReason);
    }

    private static async Task<DisclosureResult> Evaluate(
        Norm[] norms,
        DisclosureRequest request,
        RecordingAuditRepository? audit = null)
    {
        audit ??= new RecordingAuditRepository();

        var evaluator = new DisclosureEvaluator(
            new InMemoryNormRepository(norms),
            new InMemoryAttributeRepository(LegalName, ProfessionalName, SocialName, DateOfBirth),
            new NormMatcher(),
            new SpecificityRanker(),
            new TransformService(),
            new AuditWriter(audit, TimeProvider.System));

        return await evaluator.EvaluateAsync(request, CancellationToken.None);
    }

    private static DisclosureRequest Ask(string key, Purpose purpose, string? relationship = null) =>
        new(SubjectId, key, "requester-1", relationship, purpose, RequestChannel.Api);

    private static SubjectAttribute Claim(string key, string value, string label) => new()
    {
        Id = Guid.NewGuid(),
        SubjectId = SubjectId,
        Key = key,
        Value = value,
        Label = label
    };

    private static Norm Rule(
        SubjectAttribute claim,
        Purpose? purpose = null,
        string? relationship = null,
        TransformKind transform = TransformKind.None,
        string? parameter = null) => new()
    {
        Id = Guid.NewGuid(),
        Version = 1,
        SubjectId = SubjectId,
        AttributeId = claim.Id,
        Attribute = claim,
        Purpose = purpose,
        Relationship = relationship,
        Action = transform == TransformKind.None ? ActionType.Return : ActionType.Transform,
        Transform = transform,
        TransformParameter = parameter,
        JustifyingPrinciple = "Test rule."
    };
}
