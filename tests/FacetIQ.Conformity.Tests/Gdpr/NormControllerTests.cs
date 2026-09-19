using System.Security.Claims;
using FacetIQ.API.Controllers;
using FacetIQ.Contracts.Norms;
using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;
using FacetIQ.Services.Authoring;
using FacetIQ.Services.Transformation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FacetIQ.Conformity.Tests.Gdpr;

/// <summary>O8 at the endpoint: a detected conflict must also stop the norm being stored.</summary>
public class NormControllerTests
{
    private const string OwnerUserId = "owner-1";

    private static readonly Guid SubjectId = new("0a5f4d8e-0000-4000-8000-000000000001");
    private static readonly Guid StrangerSubjectId = new("0a5f4d8e-0000-4000-8000-000000000002");

    private static readonly SubjectAttribute LegalName = Claim("0a5f4d8e-0000-4000-8000-000000000010", "name", "Amara Chidinma Nwosu");
    private static readonly SubjectAttribute SocialName = Claim("0a5f4d8e-0000-4000-8000-000000000012", "name", "Amara");
    private static readonly SubjectAttribute DateOfBirth = Claim("0a5f4d8e-0000-4000-8000-000000000013", "dateOfBirth", "1994-03-11");
    private static readonly SubjectAttribute RegulatoryName = Claim("0a5f4d8e-0000-4000-8000-000000000014", "name", "Amara Chidinma Nwosu", collectedFor: Purpose.Regulatory);
    private static readonly SubjectAttribute StrangersName = Claim("0a5f4d8e-0000-4000-8000-000000000040", "name", "Someone Else", StrangerSubjectId);

    /// <summary>O8: a social request from a friend matches both at equal specificity.</summary>
    [Fact]
    public async Task CoMatchableEqualSpecificity_RejectedAtAuthoring()
    {
        var norms = new RecordingNormRepository(Existing(LegalName, purpose: Purpose.Social));
        var controller = ControllerFor(norms, Owner());

        var response = await controller.Post(
            Authoring(SocialName.Id, relationship: "friend"),
            CancellationToken.None);

        var refusal = Assert.IsType<ConflictObjectResult>(response.Result);
        var conflict = Assert.IsType<NormConflictResponse>(refusal.Value);
        var collision = Assert.Single(conflict.Collisions);

        Assert.Equal(LegalName.Id, collision.Existing.AttributeId);
        Assert.Equal(1, collision.Specificity);
        Assert.Equal("friend", collision.OverlappingRelationship);
        Assert.Equal("Social", collision.OverlappingPurpose);
        Assert.Empty(norms.Added);
    }

    /// <summary>O8: the control for the test above.</summary>
    [Fact]
    public async Task NonIntersectingEqualScore_Accepted()
    {
        var norms = new RecordingNormRepository(Existing(LegalName, purpose: Purpose.Regulatory));
        var controller = ControllerFor(norms, Owner());

        var response = await controller.Post(
            Authoring(SocialName.Id, purpose: "Social"),
            CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(response.Result);

        var stored = Assert.Single(norms.Added);
        Assert.Equal(SubjectId, stored.SubjectId);
        Assert.Equal(SocialName.Id, stored.AttributeId);
    }

    /// <summary>O8: norms on different keys never govern the same request, so they cannot tie.</summary>
    [Fact]
    public async Task NormOnAnotherKey_DoesNotCollide()
    {
        var norms = new RecordingNormRepository(Existing(DateOfBirth, purpose: Purpose.Social));
        var controller = ControllerFor(norms, Owner());

        var response = await controller.Post(
            Authoring(SocialName.Id, relationship: "friend"),
            CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(response.Result);
        Assert.Single(norms.Added);
    }

    [Fact]
    public async Task AccountOwningNoSubject_CannotAuthor()
    {
        var norms = new RecordingNormRepository();
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "stranger-1")], "test");
        var controller = ControllerFor(norms, new ClaimsPrincipal(identity));

        var response = await controller.Post(Authoring(SocialName.Id), CancellationToken.None);

        Assert.IsType<ForbidResult>(response.Result);
        Assert.Empty(norms.Added);
    }

    [Fact]
    public async Task UnrecognisedEnumValue_IsRefusedAsProblemDetails_NamingTheField()
    {
        var request = Authoring(SocialName.Id) with { Transform = "Embellish" };

        await AssertRefused(request, nameof(CreateNormRequest.Transform));
    }

    /// <summary>Enum.TryParse accepts numbers, padding and comma-joined flags; only exact names are allowed.</summary>
    [Theory]
    [InlineData("99")]
    [InlineData("1")]
    [InlineData("Regulatory,Clinical")]
    [InlineData(" Social ")]
    public async Task EnumValueThatIsNotAName_IsRefused(string purpose)
    {
        await AssertRefused(Authoring(SocialName.Id, purpose: purpose), nameof(CreateNormRequest.Purpose));
    }

    /// <summary>The other reasons belong to the engine; a rule can only refuse as itself.</summary>
    [Theory]
    [InlineData("NoMatchingNorm")]
    [InlineData("PurposeIncompatible")]
    public async Task EngineDenyReason_IsRefused(string reason)
    {
        var request = Authoring(SocialName.Id) with { DenyReason = reason };

        await AssertRefused(request, nameof(CreateNormRequest.DenyReason));
    }

    /// <summary>O5: a transform that cannot apply would otherwise fail on every matching request.</summary>
    [Theory]
    [InlineData("name", null)]
    [InlineData("dateOfBirth", "abc")]
    public async Task TransformThatCannotApplyToTheClaim_IsRefused(string key, string? parameter)
    {
        var claim = key == DateOfBirth.Key ? DateOfBirth : SocialName;
        var request = Authoring(claim.Id) with { Transform = "Generalise", TransformParameter = parameter };

        await AssertRefused(request, nameof(CreateNormRequest.Transform));
    }

    /// <summary>O2: the collection purpose would refuse this rule on every request.</summary>
    [Fact]
    public async Task ReleaseForAPurposeTheClaimWasNotCollectedFor_IsRefused()
    {
        await AssertRefused(Authoring(RegulatoryName.Id, purpose: "Social"), nameof(CreateNormRequest.Purpose));
    }

    /// <summary>O2: the controls. Each can still act on some request, so each is kept.</summary>
    [Theory]
    [InlineData("Regulatory", null)]
    [InlineData(null, null)]
    [InlineData("Social", "RefusedByRule")]
    public async Task RuleThatCanStillAct_OnAPurposeLimitedClaim_IsAccepted(string? purpose, string? denyReason)
    {
        var norms = new RecordingNormRepository();
        var request = Authoring(RegulatoryName.Id, purpose: purpose) with { DenyReason = denyReason };

        var response = await ControllerFor(norms, Owner()).Post(request, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(response.Result);
        Assert.Single(norms.Added);
    }

    [Fact]
    public async Task RemovedRule_StopsGoverning()
    {
        var rule = Existing(LegalName, purpose: Purpose.Social);
        var norms = new RecordingNormRepository(rule);

        var response = await ControllerFor(norms, Owner()).Delete(rule.Id, CancellationToken.None);

        Assert.IsType<NoContentResult>(response);
        Assert.Equal([rule.Id], norms.Retired);
        Assert.Empty(await norms.ListGoverningAsync(SubjectId, CancellationToken.None));
    }

    [Fact]
    public async Task RuleTheCallerDoesNotHold_CannotBeRemoved()
    {
        var norms = new RecordingNormRepository(Existing(LegalName, purpose: Purpose.Social));

        var response = await ControllerFor(norms, Owner()).Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(response);
        Assert.Empty(norms.Retired);
    }

    /// <summary>Missing and foreign claims get the same response, so neither is confirmed.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RuleNamingAnotherSubjectsClaim_IsRefused(bool claimExists)
    {
        var attributeId = claimExists ? StrangersName.Id : Guid.NewGuid();

        await AssertRefused(Authoring(attributeId), nameof(CreateNormRequest.AttributeId));
    }

    private static async Task AssertRefused(CreateNormRequest request, string field)
    {
        var norms = new RecordingNormRepository();

        var response = await ControllerFor(norms, Owner()).Post(request, CancellationToken.None);

        var refusal = Assert.IsType<ObjectResult>(response.Result, exactMatch: false);
        var problem = Assert.IsType<ValidationProblemDetails>(refusal.Value);

        Assert.Equal(400, refusal.StatusCode);
        Assert.True(problem.Errors.ContainsKey(field));
        Assert.Empty(norms.Added);
    }

    private static NormController ControllerFor(INormRepository norms, ClaimsPrincipal caller) =>
        new(
            new StubSubjectRepository(),
            norms,
            new InMemoryAttributeRepository(LegalName, SocialName, DateOfBirth, RegulatoryName, StrangersName),
            new ConflictDetector(),
            new TransformService(TimeProvider.System),
            TimeProvider.System)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = caller }
            }
        };

    private static ClaimsPrincipal Owner() =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, OwnerUserId)], "test"));

    private static SubjectAttribute Claim(
        string id,
        string key,
        string value,
        Guid? subjectId = null,
        Purpose? collectedFor = null) => new()
    {
        Id = new Guid(id),
        SubjectId = subjectId ?? SubjectId,
        Key = key,
        Value = value,
        CollectedFor = collectedFor
    };

    private static Norm Existing(SubjectAttribute claim, Purpose? purpose = null, string? relationship = null) => new()
    {
        Id = Guid.NewGuid(),
        Version = 1,
        SubjectId = SubjectId,
        AttributeId = claim.Id,
        Attribute = claim,
        Purpose = purpose,
        Relationship = relationship,
        Action = ActionType.Return,
        Transform = TransformKind.None,
        JustifyingPrinciple = "Already in force."
    };

    private static CreateNormRequest Authoring(
        Guid attributeId,
        string? purpose = null,
        string? relationship = null) => new()
    {
        AttributeId = attributeId,
        Purpose = purpose,
        Relationship = relationship,
        JustifyingPrinciple = "Newly authored."
    };

    private sealed class StubSubjectRepository : ISubjectRepository
    {
        private static readonly Subject Owned = new() { Id = SubjectId, UserId = OwnerUserId };

        public Task<Subject?> FindAsync(Guid subjectId, CancellationToken cancellationToken) =>
            Task.FromResult<Subject?>(subjectId == SubjectId ? Owned : null);

        public Task<Subject?> FindByUserIdAsync(string userId, CancellationToken cancellationToken) =>
            Task.FromResult<Subject?>(userId == OwnerUserId ? Owned : null);

        public Task AddAsync(Subject subject, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    /// <summary>Records writes instead of performing them, so a refusal can be shown to write nothing.</summary>
    private sealed class RecordingNormRepository : INormRepository
    {
        private readonly IReadOnlyList<Norm> _existing;

        public RecordingNormRepository(params Norm[] existing) => _existing = existing;

        public List<Norm> Added { get; } = [];

        public List<Guid> Retired { get; } = [];

        public Task<IReadOnlyList<Norm>> GetGoverningNormsAsync(
            Guid subjectId,
            string attributeKey,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Norm>>(
                InForce().Where(norm => norm.Attribute.Key == attributeKey).ToList());

        public Task<IReadOnlyList<Norm>> ListGoverningAsync(
            Guid subjectId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Norm>>(InForce().ToList());

        public Task AddAsync(Norm norm, CancellationToken cancellationToken)
        {
            Added.Add(norm);

            return Task.CompletedTask;
        }

        public Task<bool> RetireAsync(Guid id, DateTimeOffset retiredAt, CancellationToken cancellationToken)
        {
            if (Retired.Contains(id) || _existing.All(norm => norm.Id != id))
            {
                return Task.FromResult(false);
            }

            Retired.Add(id);

            return Task.FromResult(true);
        }

        private IEnumerable<Norm> InForce() => _existing.Where(norm => !Retired.Contains(norm.Id));
    }
}
