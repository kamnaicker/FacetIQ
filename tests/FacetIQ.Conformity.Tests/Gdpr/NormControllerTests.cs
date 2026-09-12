using System.Security.Claims;
using FacetIQ.API.Controllers;
using FacetIQ.Contracts.Norms;
using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;
using FacetIQ.Services.Authoring;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FacetIQ.Conformity.Tests.Gdpr;

/// <summary>
/// O8 at the boundary. The detector's tests prove it finds a collision; these prove the endpoint
/// acts on one. A detector reporting perfectly into a controller that stored the norm anyway
/// would pass every test in the services suite.
/// </summary>
public class NormControllerTests
{
    private const string OwnerUserId = "owner-1";

    private static readonly Guid SubjectId = new("0a5f4d8e-0000-4000-8000-000000000001");
    private static readonly Guid LegalName = new("0a5f4d8e-0000-4000-8000-000000000010");
    private static readonly Guid SocialName = new("0a5f4d8e-0000-4000-8000-000000000012");

    private static readonly Guid StrangerSubjectId = new("0a5f4d8e-0000-4000-8000-000000000002");
    private static readonly Guid StrangersName = new("0a5f4d8e-0000-4000-8000-000000000040");

    /// <summary>
    /// O8: a social request from a friend satisfies both rules at equal specificity, and they
    /// select different names. Refused, described, and nothing written.
    /// </summary>
    [Fact]
    public async Task CoMatchableEqualSpecificity_RejectedAtAuthoring()
    {
        var norms = new RecordingNormRepository(Existing(LegalName, purpose: Purpose.Social));
        var controller = ControllerFor(norms, Owner());

        var response = await controller.Post(
            Authoring(SocialName, relationship: "friend"),
            CancellationToken.None);

        var refusal = Assert.IsType<ConflictObjectResult>(response.Result);
        var conflict = Assert.IsType<NormConflictResponse>(refusal.Value);
        var collision = Assert.Single(conflict.Collisions);

        Assert.Equal(LegalName, collision.Existing.AttributeId);
        Assert.Equal(1, collision.Specificity);
        Assert.Equal("friend", collision.OverlappingRelationship);
        Assert.Equal("Social", collision.OverlappingPurpose);

        // Refusing and not storing are separate behaviours; only this proves the second.
        Assert.Empty(norms.Added);
    }

    /// <summary>
    /// O8, the control. The same attempt against a rule it cannot collide with is stored, so the
    /// refusal above is attributable to the conflict and not to an endpoint refusing everything.
    /// </summary>
    [Fact]
    public async Task NonIntersectingEqualScore_Accepted()
    {
        var norms = new RecordingNormRepository(Existing(LegalName, purpose: Purpose.Regulatory));
        var controller = ControllerFor(norms, Owner());

        var response = await controller.Post(
            Authoring(SocialName, purpose: "Social"),
            CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(response.Result);

        var stored = Assert.Single(norms.Added);
        Assert.Equal(SubjectId, stored.SubjectId);
        Assert.Equal(SocialName, stored.AttributeId);
    }

    /// <summary>
    /// An account with no subject bound to it cannot author anything. This is the state any
    /// newly registered account is in.
    /// </summary>
    [Fact]
    public async Task AccountOwningNoSubject_CannotAuthor()
    {
        var norms = new RecordingNormRepository();
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "stranger-1")], "test");
        var controller = ControllerFor(norms, new ClaimsPrincipal(identity));

        var response = await controller.Post(Authoring(SocialName), CancellationToken.None);

        Assert.IsType<ForbidResult>(response.Result);
        Assert.Empty(norms.Added);
    }

    /// <summary>
    /// An unrecognised enum is refused as ProblemDetails naming the field, matching the shape
    /// DataAnnotations failures already use. The key is asserted because a form binds errors to it.
    /// </summary>
    [Fact]
    public async Task UnrecognisedEnumValue_IsRefusedAsProblemDetails_NamingTheField()
    {
        var norms = new RecordingNormRepository();
        var controller = ControllerFor(norms, Owner());

        var request = Authoring(SocialName) with { Transform = "Embellish" };

        var response = await controller.Post(request, CancellationToken.None);

        var refusal = Assert.IsType<ObjectResult>(response.Result, exactMatch: false);
        var problem = Assert.IsType<ValidationProblemDetails>(refusal.Value);

        Assert.Equal(400, refusal.StatusCode);
        Assert.True(problem.Errors.ContainsKey(nameof(CreateNormRequest.Transform)));
        Assert.Empty(norms.Added);
    }

    /// <summary>
    /// Removing a rule retires it: it stops governing, and the repository is asked to retire
    /// rather than delete, so audit records that name it keep their meaning.
    /// </summary>
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

    /// <summary>
    /// A rule may only select the author's own claim. The foreign key proves the claim exists,
    /// not who holds it; without this the engine would release another subject's value under
    /// the author's rules. Missing and foreign are refused identically, so the response does
    /// not confirm that an id exists.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RuleNamingAnotherSubjectsClaim_IsRefused(bool claimExists)
    {
        var norms = new RecordingNormRepository();
        var controller = ControllerFor(norms, Owner());
        var attributeId = claimExists ? StrangersName : Guid.NewGuid();

        var response = await controller.Post(Authoring(attributeId), CancellationToken.None);

        var refusal = Assert.IsType<ObjectResult>(response.Result, exactMatch: false);
        var problem = Assert.IsType<ValidationProblemDetails>(refusal.Value);

        Assert.Equal(400, refusal.StatusCode);
        Assert.True(problem.Errors.ContainsKey(nameof(CreateNormRequest.AttributeId)));
        Assert.Empty(norms.Added);
    }

    private static NormController ControllerFor(INormRepository norms, ClaimsPrincipal caller) =>
        new(new StubSubjectRepository(), norms, Claims(), new ConflictDetector(), TimeProvider.System)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = caller }
            }
        };

    private static ClaimsPrincipal Owner() =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, OwnerUserId)], "test"));

    /// <summary>The owner's two names, and one name held by someone else.</summary>
    private static InMemoryAttributeRepository Claims() => new(
        Claim(LegalName, SubjectId, "Amara Chidinma Nwosu"),
        Claim(SocialName, SubjectId, "Amara"),
        Claim(StrangersName, StrangerSubjectId, "Someone Else"));

    private static SubjectAttribute Claim(Guid id, Guid subjectId, string value) => new()
    {
        Id = id,
        SubjectId = subjectId,
        Key = "name",
        Value = value
    };

    private static Norm Existing(Guid attributeId, Purpose? purpose = null, string? relationship = null) => new()
    {
        Id = Guid.NewGuid(),
        Version = 1,
        SubjectId = SubjectId,
        AttributeId = attributeId,
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

    /// <summary>Holds the single subject the owner account owns, and nothing for anyone else.</summary>
    private sealed class StubSubjectRepository : ISubjectRepository
    {
        private static readonly Subject Owned = new() { Id = SubjectId, UserId = OwnerUserId };

        public Task<Subject?> FindAsync(Guid subjectId, CancellationToken cancellationToken) =>
            Task.FromResult<Subject?>(subjectId == SubjectId ? Owned : null);

        public Task<Subject?> FindByUserIdAsync(string userId, CancellationToken cancellationToken) =>
            Task.FromResult<Subject?>(userId == OwnerUserId ? Owned : null);

        public Task AddAsync(Subject subject, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Authoring norms never creates a subject.");
    }

    /// <summary>
    /// Records writes rather than performing them, so a test can assert a refusal wrote nothing.
    /// </summary>
    private sealed class RecordingNormRepository : INormRepository
    {
        private readonly IReadOnlyList<Norm> _existing;

        public RecordingNormRepository(params Norm[] existing) => _existing = existing;

        public List<Norm> Added { get; } = [];

        public List<Guid> Retired { get; } = [];

        public Task<IReadOnlyList<Norm>> ListGoverningAsync(
            Guid subjectId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Norm>>(
                _existing.Where(norm => !Retired.Contains(norm.Id)).ToList());

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

        public Task<IReadOnlyList<Norm>> GetGoverningNormsAsync(
            Guid subjectId,
            string attributeKey,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("Authoring never reads norms by key.");
    }
}
