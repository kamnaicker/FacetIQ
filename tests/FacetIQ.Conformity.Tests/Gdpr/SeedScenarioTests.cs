using FacetIQ.Data.Seeding;
using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;
using FacetIQ.Domain.Models;
using FacetIQ.Services.Auditing;
using FacetIQ.Services.Disclosure;
using FacetIQ.Services.Matching;
using FacetIQ.Services.Transformation;

namespace FacetIQ.Conformity.Tests.Gdpr;

/// <summary>
/// The seeded profile is what a reader meets first, so it is checked against the rules it ships
/// with rather than described. Every case below runs the real seed data through the engine.
/// </summary>
public class SeedScenarioTests
{
    private const string Stranger = "seed-stranger";

    /// <summary>O9: the seeded social rule names no relationship, so a social enquiry is answered.</summary>
    [Fact]
    public async Task SocialName_IsAnsweredForAnyone()
    {
        var result = await Ask("name", Purpose.Social, Stranger);

        Assert.Equal(ActionType.Return, result.Outcome);
        Assert.Equal("Amara", result.Value);
    }

    /// <summary>The professional name is bound to a colleague standing, which a stranger does not hold.</summary>
    [Fact]
    public async Task ProfessionalName_IsRefusedToAStranger()
    {
        var result = await Ask("name", Purpose.Clinical, Stranger);

        Assert.Equal(ActionType.Deny, result.Outcome);
        Assert.Equal(DenyReasonCode.NoMatchingNorm, result.DenyReason);
    }

    /// <summary>A standing that has been issued but not accepted must not unlock anything.</summary>
    [Fact]
    public async Task ProfessionalName_IsRefusedWhileTheStandingIsPending()
    {
        var result = await Ask("name", Purpose.Clinical, SeedData.PendingColleagueUserId);

        Assert.Equal(ActionType.Deny, result.Outcome);
        Assert.Equal(DenyReasonCode.NoMatchingNorm, result.DenyReason);
    }

    /// <summary>The control: the same request from the accepted colleague is answered.</summary>
    [Fact]
    public async Task ProfessionalName_IsAnsweredToTheAcceptedColleague()
    {
        var result = await Ask("name", Purpose.Clinical, SeedData.AcceptedColleagueUserId);

        Assert.Equal(ActionType.Return, result.Outcome);
        Assert.Equal("Dr Amara Nwosu", result.Value);
    }

    /// <summary>O2: the seeded date of birth was collected for social use, so the regulatory rule cannot release it.</summary>
    [Fact]
    public async Task DateOfBirth_IsRefusedForRegulatoryReasons()
    {
        var result = await Ask("dateOfBirth", Purpose.Regulatory, Stranger);

        Assert.Equal(ActionType.Deny, result.Outcome);
        Assert.Equal(DenyReasonCode.PurposeIncompatible, result.DenyReason);
    }

    /// <summary>O4: the social rule answers with a threshold rather than the date.</summary>
    [Fact]
    public async Task DateOfBirth_IsAnsweredAsAThreshold()
    {
        var result = await Ask("dateOfBirth", Purpose.Social, Stranger);

        Assert.Equal(ActionType.Transform, result.Outcome);
        Assert.Equal("over 18", result.Value);
    }

    // The database fills the claim behind each rule; here it is attached by hand.
    private static Norm[] SeededNorms()
    {
        var claims = SeedData.Attributes.ToDictionary(claim => claim.Id);

        return SeedData.Norms
            .Select(norm => new Norm
            {
                Id = norm.Id,
                Version = norm.Version,
                SupersededAt = norm.SupersededAt,
                SubjectId = norm.SubjectId,
                AttributeId = norm.AttributeId,
                Attribute = claims[norm.AttributeId],
                Relationship = norm.Relationship,
                Purpose = norm.Purpose,
                Action = norm.Action,
                Transform = norm.Transform,
                TransformParameter = norm.TransformParameter,
                DenyReason = norm.DenyReason,
                JustifyingPrinciple = norm.JustifyingPrinciple,
            })
            .ToArray();
    }

    private static async Task<DisclosureResult> Ask(string key, Purpose purpose, string requesterUserId)
    {
        var evaluator = new DisclosureEvaluator(
            new InMemorySubjectRepository(SeedData.Subjects[0]),
            new InMemoryNormRepository(SeededNorms()),
            new InMemoryStandingRepository(SeedData.Standings),
            new InMemoryAttributeRepository(SeedData.Attributes),
            new NormMatcher(),
            new SpecificityRanker(),
            new TransformService(TimeProvider.System),
            new AuditWriter(new RecordingAuditRepository(), TimeProvider.System));

        var request = new DisclosureRequest(
            SeedData.SubjectId,
            key,
            requesterUserId,
            purpose,
            RequestChannel.Api);

        return await evaluator.EvaluateAsync(request, CancellationToken.None);
    }
}
