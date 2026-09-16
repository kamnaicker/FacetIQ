using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;
using FacetIQ.Domain.Models;
using FacetIQ.Services.Auditing;
using FacetIQ.Services.Disclosure;
using FacetIQ.Services.Matching;
using FacetIQ.Services.Transformation;

namespace FacetIQ.Conformity.Tests.Gdpr;

/// <summary>Each test checks one project objective (O-tag) against the decision and its audit record.</summary>
public class DisclosureEvaluatorTests
{
    private const string RequesterUserId = "requester-1";

    private static readonly Guid SubjectId = new("0a5f4d8e-0000-4000-8000-000000000001");

    private static readonly SubjectAttribute LegalName = Claim("name", "Amara Chidinma Nwosu", "legal");
    private static readonly SubjectAttribute ProfessionalName = Claim("name", "Dr Amara Nwosu", "professional");
    private static readonly SubjectAttribute SocialName = Claim("name", "Amara", "social");
    private static readonly SubjectAttribute DateOfBirth = Claim("dateOfBirth", "1994-03-11", "legal", Purpose.Social);

    // Separate from the evaluator's instance so the replay is independent.
    private static readonly TransformService Transforms = new(TimeProvider.System);

    /// <summary>O1: the norm here would reformat one name, so getting all three proves norms were skipped.</summary>
    [Fact]
    public async Task SelfAccess_OwnAttributes_ReturnsAllUntransformed()
    {
        var audit = new RecordingAuditRepository();
        var owner = new Subject { Id = SubjectId, UserId = RequesterUserId };
        var norms = new[] { Rule(SocialName, purpose: Purpose.Social, transform: TransformKind.Reformat) };

        var result = await Evaluate(norms, Ask("name", Purpose.Social), audit, owner);

        Assert.Equal(ActionType.Return, result.Outcome);
        Assert.Equal(new[] { "Amara Chidinma Nwosu", "Dr Amara Nwosu", "Amara" }, result.Values);
        Assert.Null(result.Value);

        var record = Assert.Single(audit.Written);
        Assert.True(record.Transform is null or TransformKind.None);
    }

    /// <summary>O9: one key, three contexts, three different claims selected.</summary>
    [Fact]
    public async Task SameAttribute_ThreeContexts_ThreeRepresentations()
    {
        var norms = new[]
        {
            Rule(LegalName, purpose: Purpose.Regulatory),
            Rule(ProfessionalName, purpose: Purpose.Clinical, relationship: "colleague"),
            Rule(SocialName, purpose: Purpose.Social)
        };

        var regulatory = await Evaluate(norms, Ask("name", Purpose.Regulatory));
        var clinical = await Evaluate(norms, Ask("name", Purpose.Clinical), standings: [Held("colleague")]);
        var social = await Evaluate(norms, Ask("name", Purpose.Social));

        Assert.Equal("Amara Chidinma Nwosu", regulatory.Value);
        Assert.Equal("Dr Amara Nwosu", clinical.Value);
        Assert.Equal("Amara", social.Value);
    }

    /// <summary>O10: only the relationship differs between the two requests.</summary>
    [Fact]
    public async Task SingleParameterChange_ChangesRepresentation()
    {
        var norms = new[]
        {
            Rule(SocialName, purpose: Purpose.Clinical),
            Rule(ProfessionalName, purpose: Purpose.Clinical, relationship: "colleague")
        };

        var withoutRelationship = await Evaluate(norms, Ask("name", Purpose.Clinical));
        var asColleague = await Evaluate(norms, Ask("name", Purpose.Clinical), standings: [Held("colleague")]);

        Assert.Equal("Amara", withoutRelationship.Value);
        Assert.Equal("Dr Amara Nwosu", asColleague.Value);
    }

    /// <summary>O8: two applicable norms at equal specificity are refused, not tie-broken.</summary>
    [Fact]
    public async Task EquallySpecificNorms_AreRefusedAsAmbiguous()
    {
        var norms = new[]
        {
            Rule(LegalName, purpose: Purpose.Social),
            Rule(SocialName, relationship: "friend")
        };

        var result = await Evaluate(norms, Ask("name", Purpose.Social), standings: [Held("friend")]);

        Assert.Equal(ActionType.Deny, result.Outcome);
        Assert.Equal(DenyReasonCode.AmbiguousNorms, result.DenyReason);
    }

    /// <summary>O8: the control. Equal scores with disjoint conditions never compete.</summary>
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

        var result = await Evaluate(norms, Ask("name", Purpose.Clinical), standings: [Held("colleague")]);

        Assert.Equal("Dr Amara Nwosu", result.Value);
    }

    /// <summary>A requester can hold several standings; a norm bound to any one of them applies.</summary>
    [Fact]
    public async Task NormBoundToOneHeldStanding_AppliesWhenSeveralAreHeld()
    {
        var norms = new[] { Rule(ProfessionalName, purpose: Purpose.Clinical, relationship: "colleague") };

        var result = await Evaluate(
            norms,
            Ask("name", Purpose.Clinical),
            standings: [Held("friend"), Held("colleague")]);

        Assert.Equal("Dr Amara Nwosu", result.Value);
    }

    /// <summary>A pending standing behaves the same as none.</summary>
    [Fact]
    public async Task AcceptedStanding_UnlocksTheNorm_PendingAndAbsentDoNot()
    {
        var norms = new[]
        {
            Rule(SocialName),
            Rule(ProfessionalName, purpose: Purpose.Clinical, relationship: "colleague")
        };

        var accepted = await Evaluate(
            norms,
            Ask("name", Purpose.Clinical),
            standings: [Held("colleague")]);

        var pending = await Evaluate(
            norms,
            Ask("name", Purpose.Clinical),
            standings: [Held("colleague", accepted: false)]);

        var absent = await Evaluate(norms, Ask("name", Purpose.Clinical));

        Assert.Equal("Dr Amara Nwosu", accepted.Value);
        Assert.Equal("Amara", pending.Value);
        Assert.Equal("Amara", absent.Value);
    }

    /// <summary>O2: the collection purpose overrides a norm that matches.</summary>
    [Fact]
    public async Task IncompatiblePurpose_ReturnsDeny()
    {
        var norms = new[] { Rule(DateOfBirth, purpose: Purpose.Regulatory) };

        var result = await Evaluate(norms, Ask("dateOfBirth", Purpose.Regulatory));

        Assert.Equal(ActionType.Deny, result.Outcome);
        Assert.Equal(DenyReasonCode.PurposeIncompatible, result.DenyReason);
        Assert.Null(result.Value);
    }

    /// <summary>A rule's refusal is recorded as RefusedByRule, distinct from engine refusals.</summary>
    [Fact]
    public async Task AuthoredRefusal_IsRecordedAsRefusedByRule()
    {
        var refusal = Refusal(LegalName, purpose: Purpose.Social);

        var result = await Evaluate([refusal], Ask("name", Purpose.Social));

        Assert.Equal(ActionType.Deny, result.Outcome);
        Assert.Equal(DenyReasonCode.RefusedByRule, result.DenyReason);
        Assert.Same(refusal, result.Norm);
    }

    /// <summary>Only the engine may claim it refused, so a stored reason on a rule is not trusted.</summary>
    [Fact]
    public async Task AuthoredRefusal_CannotRecordAnEngineReason()
    {
        var refusal = Refusal(LegalName, purpose: Purpose.Social, reason: DenyReasonCode.NoMatchingNorm);

        var result = await Evaluate([refusal], Ask("name", Purpose.Social));

        Assert.Equal(DenyReasonCode.RefusedByRule, result.DenyReason);
    }

    /// <summary>O5: a transform that cannot apply is refused, not thrown, so it is still audited.</summary>
    [Fact]
    public async Task TransformThatCannotApply_IsRefusedAndAudited()
    {
        var audit = new RecordingAuditRepository();
        var norms = new[] { Rule(SocialName, purpose: Purpose.Social, transform: TransformKind.Generalise) };

        var result = await Evaluate(norms, Ask("name", Purpose.Social), audit);

        Assert.Equal(ActionType.Deny, result.Outcome);
        Assert.Equal(DenyReasonCode.TransformFailed, result.DenyReason);
        Assert.Null(result.Value);

        var record = Assert.Single(audit.Written);
        Assert.Equal(DenyReasonCode.TransformFailed, record.DenyReason);
        Assert.Equal(norms[0].Id, record.NormId);
    }

    /// <summary>O7: no applicable norm means Deny.</summary>
    [Fact]
    public async Task NoMatchingNorm_ReturnsDeny()
    {
        var norms = new[] { Rule(LegalName, purpose: Purpose.Regulatory) };

        var result = await Evaluate(norms, Ask("name", Purpose.Social));

        Assert.Equal(ActionType.Deny, result.Outcome);
        Assert.Equal(DenyReasonCode.NoMatchingNorm, result.DenyReason);
        Assert.Null(result.Value);
    }

    /// <summary>O4: a transform releases a coarser value that was never stored.</summary>
    [Fact]
    public async Task TransformNorm_ReturnsCoarserTruthfulValue()
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

    /// <summary>O5: one shared audit log, so a missing or duplicate write shows in the count.</summary>
    [Fact]
    public async Task EveryOutcome_WritesExactlyOneAuditRecord()
    {
        var audit = new RecordingAuditRepository();
        var norms = new[]
        {
            Rule(SocialName, purpose: Purpose.Social),
            Rule(DateOfBirth, purpose: Purpose.Social, transform: TransformKind.Generalise, parameter: "18")
        };

        await Evaluate(norms, Ask("name", Purpose.Social), audit);
        Assert.Equal(ActionType.Return, Assert.Single(audit.Written).Outcome);

        await Evaluate(norms, Ask("dateOfBirth", Purpose.Social), audit);
        Assert.Equal(2, audit.Written.Count);
        Assert.Equal(ActionType.Transform, audit.Written[1].Outcome);

        await Evaluate(norms, Ask("name", Purpose.Regulatory), audit);
        Assert.Equal(3, audit.Written.Count);
        Assert.Equal(ActionType.Deny, audit.Written[2].Outcome);
    }

    /// <summary>O6: replaying the recorded transform on the stored claim reproduces the value.</summary>
    [Fact]
    public async Task AuditRecord_ReconstructsDisclosure()
    {
        var audit = new RecordingAuditRepository();
        var norms = new[]
        {
            Rule(DateOfBirth, purpose: Purpose.Social, transform: TransformKind.Generalise, parameter: "18")
        };

        var result = await Evaluate(norms, Ask("dateOfBirth", Purpose.Social), audit);

        var record = Assert.Single(audit.Written);
        Assert.Equal(norms[0].Id, record.NormId);
        Assert.Equal(norms[0].Version, record.NormVersion);
        Assert.Equal(TransformKind.Generalise, record.Transform);
        Assert.Equal("18", record.TransformParameter);

        Assert.True(Transforms.TryApply(record.Transform!.Value, record.TransformParameter, DateOfBirth.Value, out var replayed));
        Assert.Equal(result.Value, replayed);
    }

    /// <summary>O6: checks every property, so a field added later that copies the value fails.</summary>
    [Fact]
    public async Task AuditRecord_ContainsNoDisclosedValue()
    {
        var audit = new RecordingAuditRepository();
        var norms = new[] { Rule(ProfessionalName, purpose: Purpose.Clinical, relationship: "colleague") };

        var result = await Evaluate(norms, Ask("name", Purpose.Clinical), audit, standings: [Held("colleague")]);

        Assert.Equal("Dr Amara Nwosu", result.Value);

        var record = Assert.Single(audit.Written);
        var fields = typeof(AuditRecord)
            .GetProperties()
            .Select(property => property.GetValue(record)?.ToString() ?? string.Empty);

        Assert.All(fields, field => Assert.DoesNotContain(result.Value!, field));
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
        RecordingAuditRepository? audit = null,
        Subject? owner = null,
        Standing[]? standings = null)
    {
        audit ??= new RecordingAuditRepository();

        var evaluator = new DisclosureEvaluator(
            new InMemorySubjectRepository(owner),
            new InMemoryNormRepository(norms),
            new InMemoryStandingRepository(standings ?? []),
            new InMemoryAttributeRepository(LegalName, ProfessionalName, SocialName, DateOfBirth),
            new NormMatcher(),
            new SpecificityRanker(),
            new TransformService(TimeProvider.System),
            new AuditWriter(audit, TimeProvider.System));

        return await evaluator.EvaluateAsync(request, CancellationToken.None);
    }

    private static DisclosureRequest Ask(string key, Purpose purpose) =>
        new(SubjectId, key, RequesterUserId, purpose, RequestChannel.Api);

    /// <summary>Accepted unless <c>accepted: false</c>.</summary>
    private static Standing Held(string value, bool accepted = true) => new()
    {
        Id = Guid.NewGuid(),
        SubjectId = SubjectId,
        RequesterUserId = RequesterUserId,
        Value = value,
        IssuerKind = IssuerKind.Institution,
        Issuer = "Example Teaching Hospital",
        IssuedAt = DateTimeOffset.UnixEpoch,
        AcceptedAt = accepted ? DateTimeOffset.UnixEpoch : null
    };

    private static SubjectAttribute Claim(
        string key,
        string value,
        string label,
        Purpose? collectedFor = null) => new()
    {
        Id = Guid.NewGuid(),
        SubjectId = SubjectId,
        Key = key,
        Value = value,
        Label = label,
        CollectedFor = collectedFor
    };

    private static Norm Refusal(
        SubjectAttribute claim,
        Purpose? purpose = null,
        DenyReasonCode reason = DenyReasonCode.RefusedByRule) => new()
    {
        Id = Guid.NewGuid(),
        Version = 1,
        SubjectId = SubjectId,
        AttributeId = claim.Id,
        Attribute = claim,
        Purpose = purpose,
        Action = ActionType.Deny,
        Transform = TransformKind.None,
        DenyReason = reason,
        JustifyingPrinciple = "Test refusal."
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
