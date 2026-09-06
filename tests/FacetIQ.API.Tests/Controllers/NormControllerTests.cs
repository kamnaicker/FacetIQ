using System.Security.Claims;
using FacetIQ.API.Controllers;
using FacetIQ.Contracts.Norms;
using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;
using FacetIQ.Services.Authoring;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FacetIQ.API.Tests.Controllers;

/// <summary>
/// O8 at the boundary. The detector's own tests prove it identifies a collision; these prove the
/// endpoint acts on one -- that the author is told what their rule collided with, and that nothing
/// was written. A detector reporting perfectly into a controller that stored the norm anyway would
/// pass every test in the services suite.
/// </summary>
public class NormControllerTests
{
    private const string OwnerUserId = "owner-1";

    private static readonly Guid SubjectId = new("0a5f4d8e-0000-4000-8000-000000000001");
    private static readonly Guid LegalName = new("0a5f4d8e-0000-4000-8000-000000000010");
    private static readonly Guid SocialName = new("0a5f4d8e-0000-4000-8000-000000000012");

    /// <summary>
    /// O8: a social request from a friend would satisfy both rules at equal specificity, and they
    /// select different names. The norm is refused, the collision is described back with the
    /// request that witnesses it, and the store is left untouched.
    /// </summary>
    [Fact]
    public async Task ConflictingNorm_IsRefused_AndNeverPersisted()
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

        // The clause the detector's own tests cannot reach: refusing and not storing are two
        // separate behaviours, and only this one proves the second.
        Assert.Empty(norms.Added);
    }

    /// <summary>
    /// The control. The same authoring attempt against a rule it cannot collide with is stored,
    /// so the refusal above is attributable to the conflict rather than to an endpoint that
    /// refuses everything.
    /// </summary>
    [Fact]
    public async Task NonConflictingNorm_IsStored()
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
    /// An account with no subject bound to it cannot author anything. This is the state every
    /// registered account is in today, and it is why the norm endpoints cannot yet be exercised
    /// over HTTP.
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

    private static NormController ControllerFor(INormRepository norms, ClaimsPrincipal caller) =>
        new(new StubSubjectRepository(), norms, new ConflictDetector())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = caller }
            }
        };

    private static ClaimsPrincipal Owner() =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, OwnerUserId)], "test"));

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
    }

    /// <summary>
    /// Records writes rather than performing them, so a test can assert that a refusal wrote
    /// nothing. The norms it was constructed with are what the detector compares against.
    /// </summary>
    private sealed class RecordingNormRepository : INormRepository
    {
        private readonly IReadOnlyList<Norm> _existing;

        public RecordingNormRepository(params Norm[] existing) => _existing = existing;

        public List<Norm> Added { get; } = [];

        public Task<IReadOnlyList<Norm>> ListGoverningAsync(
            Guid subjectId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_existing);

        public Task AddAsync(Norm norm, CancellationToken cancellationToken)
        {
            Added.Add(norm);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Norm>> GetGoverningNormsAsync(
            Guid subjectId,
            string attributeKey,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("Authoring never reads norms by key.");
    }
}
