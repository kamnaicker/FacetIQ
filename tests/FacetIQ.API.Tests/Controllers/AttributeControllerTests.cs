using System.Security.Claims;
using FacetIQ.API.Controllers;
using FacetIQ.Contracts.Attributes;
using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;
using FacetIQ.Services.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FacetIQ.API.Tests.Controllers;

public class AttributeControllerTests
{
    private const string OwnerUserId = "owner-1";

    private static readonly Guid SubjectId = new("0a5f4d8e-0000-4000-8000-000000000201");

    [Fact]
    public async Task DuplicateClaim_IsRefused_AndNotStored()
    {
        var claims = new Claims(Held("Sam"));

        var response = await ControllerFor(claims, new Norms()).Post(Adding("Sam"), CancellationToken.None);

        var refusal = Assert.IsType<ObjectResult>(response.Result, exactMatch: false);
        var problem = Assert.IsType<ValidationProblemDetails>(refusal.Value);
        Assert.Equal(400, refusal.StatusCode);
        Assert.True(problem.Errors.ContainsKey(nameof(CreateAttributeRequest.Value)));
        Assert.Single(claims.Rows);
    }

    /// <summary>Names are not case-normalised.</summary>
    [Fact]
    public async Task DifferentCapitalisation_IsADifferentClaim()
    {
        var claims = new Claims(Held("Sam"));

        var response = await ControllerFor(claims, new Norms()).Post(Adding("SAM"), CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(response.Result);
        Assert.Equal(2, claims.Rows.Count);
    }

    [Fact]
    public async Task ValueThatDoesNotSuitItsKind_IsRefused_AndNotStored()
    {
        var claims = new Claims();

        var response = await ControllerFor(claims, new Norms()).Post(
            new CreateAttributeRequest { Key = "email", Value = "banana" },
            CancellationToken.None);

        var refusal = Assert.IsType<ObjectResult>(response.Result, exactMatch: false);
        var problem = Assert.IsType<ValidationProblemDetails>(refusal.Value);
        Assert.Equal(400, refusal.StatusCode);
        Assert.True(problem.Errors.ContainsKey(nameof(CreateAttributeRequest.Value)));
        Assert.Empty(claims.Rows);
    }

    [Fact]
    public async Task ClaimReleasedByALiveRule_CannotBeDeleted_AndTheRuleIsNamed()
    {
        var claim = Held("Sam");
        var rule = RuleFor(claim);
        var claims = new Claims(claim);

        var response = await ControllerFor(claims, new Norms(rule)).Delete(claim.Id, CancellationToken.None);

        var refusal = Assert.IsType<ConflictObjectResult>(response);
        var body = Assert.IsType<ClaimInUseResponse>(refusal.Value);
        Assert.Equal(rule.Id, Assert.Single(body.Rules).Id);
        Assert.Single(claims.Rows);
    }

    /// <summary>The control for the test above.</summary>
    [Fact]
    public async Task ClaimNoLiveRuleReleases_IsDeleted()
    {
        var claim = Held("Sam");
        var claims = new Claims(claim);

        var response = await ControllerFor(claims, new Norms()).Delete(claim.Id, CancellationToken.None);

        Assert.IsType<NoContentResult>(response);
        Assert.Empty(claims.Rows);
    }

    private static AttributeController ControllerFor(Claims claims, Norms norms) =>
        new(new Subjects(), claims, norms, new ClaimValueValidator(TimeProvider.System))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, OwnerUserId)], "test")),
                },
            },
        };

    private static SubjectAttribute Held(string value) => new()
    {
        Id = Guid.NewGuid(),
        SubjectId = SubjectId,
        Key = "name",
        Value = value,
    };

    private static CreateAttributeRequest Adding(string value) => new() { Key = "name", Value = value };

    private static Norm RuleFor(SubjectAttribute claim) => new()
    {
        Id = Guid.NewGuid(),
        Version = 1,
        SubjectId = SubjectId,
        AttributeId = claim.Id,
        Purpose = Purpose.Social,
        Action = ActionType.Return,
        Transform = TransformKind.None,
        JustifyingPrinciple = "Friends call me Sam.",
    };

    private sealed class Subjects : ISubjectRepository
    {
        private static readonly Subject Owner = new() { Id = SubjectId, UserId = OwnerUserId };

        public Task<Subject?> FindAsync(Guid subjectId, CancellationToken cancellationToken) =>
            Task.FromResult<Subject?>(subjectId == SubjectId ? Owner : null);

        public Task<Subject?> FindByUserIdAsync(string userId, CancellationToken cancellationToken) =>
            Task.FromResult<Subject?>(userId == OwnerUserId ? Owner : null);

        public Task AddAsync(Subject subject, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class Claims(params SubjectAttribute[] held) : IAttributeRepository
    {
        public List<SubjectAttribute> Rows { get; } = [.. held];

        public Task<SubjectAttribute?> FindAsync(Guid attributeId, CancellationToken cancellationToken) =>
            Task.FromResult(Rows.SingleOrDefault(claim => claim.Id == attributeId));

        public Task<IReadOnlyList<SubjectAttribute>> ListByKeyAsync(
            Guid subjectId,
            string key,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SubjectAttribute>>(
                Rows.Where(claim => claim.SubjectId == subjectId && claim.Key == key).ToList());

        public Task<IReadOnlyList<SubjectAttribute>> ListBySubjectAsync(
            Guid subjectId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SubjectAttribute>>(
                Rows.Where(claim => claim.SubjectId == subjectId).ToList());

        public Task AddAsync(SubjectAttribute attribute, CancellationToken cancellationToken)
        {
            Rows.Add(attribute);

            return Task.CompletedTask;
        }

        public Task<bool> DeleteAsync(Guid attributeId, CancellationToken cancellationToken) =>
            Task.FromResult(Rows.RemoveAll(claim => claim.Id == attributeId) > 0);
    }

    private sealed class Norms(params Norm[] governing) : INormRepository
    {
        public Task<IReadOnlyList<Norm>> ListGoverningAsync(
            Guid subjectId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Norm>>(governing);

        public Task<IReadOnlyList<Norm>> GetGoverningNormsAsync(
            Guid subjectId,
            string attributeKey,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task AddAsync(Norm norm, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> RetireAsync(Guid id, DateTimeOffset retiredAt, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
