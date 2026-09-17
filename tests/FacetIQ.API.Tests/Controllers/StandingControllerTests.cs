using System.Security.Claims;
using FacetIQ.API.Controllers;
using FacetIQ.Contracts.Standings;
using FacetIQ.Data.Identity;
using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FacetIQ.API.Tests.Controllers;

/// <summary>Issuing always creates a pending standing, and only the holder can accept it.</summary>
public class StandingControllerTests
{
    private const string SamUserId = "sam";
    private const string RiyaUserId = "riya";
    private const string SamEmail = "sam@example.test";
    private const string RiyaEmail = "riya@example.test";

    private static readonly Guid SamSubjectId = new("0a5f4d8e-0000-4000-8000-000000000101");
    private static readonly DateTimeOffset Now = new(2026, 9, 11, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task IssuedStanding_IsPending_UntilTheHolderAccepts()

    {
        var standings = new InMemoryStandings();

        var response = await ControllerFor(SamUserId, standings)
            .Post(Issue(RiyaEmail), CancellationToken.None);

        Assert.IsType<OkObjectResult>(response.Result);

        var stored = Assert.Single(standings.Rows);
        Assert.Equal(SamSubjectId, stored.SubjectId);
        Assert.Equal(RiyaUserId, stored.RequesterUserId);
        Assert.Equal(IssuerKind.Subject, stored.IssuerKind);
        Assert.Equal(SamUserId, stored.Issuer);
        Assert.Null(stored.AcceptedAt);

        Assert.Empty(await standings.GetAcceptedAsync(SamSubjectId, RiyaUserId, CancellationToken.None));
    }

    /// <summary>Answered as 404, the same as a standing that does not exist.</summary>
    [Fact]
    public async Task IssuerCannotAcceptTheirOwnStanding()
    {
        var standings = new InMemoryStandings();
        await ControllerFor(SamUserId, standings).Post(Issue(RiyaEmail), CancellationToken.None);
        var id = Assert.Single(standings.Rows).Id;

        var response = await ControllerFor(SamUserId, standings).Accept(id, CancellationToken.None);

        Assert.IsType<NotFoundResult>(response);
        Assert.Empty(await standings.GetAcceptedAsync(SamSubjectId, RiyaUserId, CancellationToken.None));
    }

    /// <summary>The control for the test above.</summary>
    [Fact]
    public async Task HolderAccepting_BringsTheStandingIntoDecisions()
    {
        var standings = new InMemoryStandings();
        await ControllerFor(SamUserId, standings).Post(Issue(RiyaEmail), CancellationToken.None);
        var id = Assert.Single(standings.Rows).Id;

        var response = await ControllerFor(RiyaUserId, standings).Accept(id, CancellationToken.None);

        Assert.IsType<NoContentResult>(response);

        var accepted = Assert.Single(
            await standings.GetAcceptedAsync(SamSubjectId, RiyaUserId, CancellationToken.None));
        Assert.Equal(Now, accepted.AcceptedAt);
    }

    [Fact]
    public async Task StandingCannotDescribeTheIssuer()
    {
        var standings = new InMemoryStandings();

        var response = await ControllerFor(SamUserId, standings)
            .Post(Issue(SamEmail), CancellationToken.None);

        AssertRefusedOnEmail(response.Result);
        Assert.Empty(standings.Rows);
    }

    [Fact]
    public async Task UnknownAddress_IsRefused_AndNothingIsStored()
    {
        var standings = new InMemoryStandings();

        var response = await ControllerFor(SamUserId, standings)
            .Post(Issue("nobody@example.test"), CancellationToken.None);

        AssertRefusedOnEmail(response.Result);
        Assert.Empty(standings.Rows);
    }

    [Fact]
    public async Task SameTermForTheSamePerson_IsRefusedTheSecondTime()
    {
        var standings = new InMemoryStandings();
        standings.Rows.Add(Pending(RiyaUserId, "colleague"));

        var response = await ControllerFor(SamUserId, standings)
            .Post(Issue(RiyaEmail) with { Value = "Colleague" }, CancellationToken.None);

        var refusal = Assert.IsType<ObjectResult>(response.Result, exactMatch: false);
        var problem = Assert.IsType<ValidationProblemDetails>(refusal.Value);

        Assert.Equal(400, refusal.StatusCode);
        Assert.True(problem.Errors.ContainsKey(nameof(IssueStandingRequest.Value)));
        Assert.Single(standings.Rows);
    }

    /// <summary>Regression: concurrent email lookups failed on the shared DbContext. StubUsers throws on overlap.</summary>
    [Fact]
    public async Task SeveralStandings_AreListed()
    {
        var standings = new InMemoryStandings();
        standings.Rows.Add(Pending(RiyaUserId, "colleague"));
        standings.Rows.Add(Pending(RiyaUserId, "friend"));
        standings.Rows.Add(Pending(RiyaUserId, "neighbour"));

        var response = await ControllerFor(SamUserId, standings).Get(CancellationToken.None);

        var listed = Assert.IsType<StandingsResponse>(Assert.IsType<OkObjectResult>(response.Result).Value);
        Assert.Equal(3, listed.Issued.Count);
        Assert.All(listed.Issued, standing => Assert.Equal(RiyaEmail, standing.Holder));
    }

    private static Standing Pending(string holder, string value) => new()
    {
        Id = Guid.NewGuid(),
        SubjectId = SamSubjectId,
        RequesterUserId = holder,
        Value = value,
        IssuerKind = IssuerKind.Subject,
        Issuer = SamUserId,
        IssuedAt = Now,
    };

    private static void AssertRefusedOnEmail(ActionResult? result)
    {
        var refusal = Assert.IsType<ObjectResult>(result, exactMatch: false);
        var problem = Assert.IsType<ValidationProblemDetails>(refusal.Value);

        Assert.Equal(400, refusal.StatusCode);
        Assert.True(problem.Errors.ContainsKey(nameof(IssueStandingRequest.Email)));
    }

    private static StandingController ControllerFor(string userId, InMemoryStandings standings) =>
        new StandingController(standings, new Subjects(), new SingleContextUserDirectory(), new FixedClock())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "test")),
                },
            },
        };

    private static IssueStandingRequest Issue(string email) => new()
    {
        Email = email,
        Value = "colleague",
    };

    private sealed class InMemoryStandings : IStandingRepository
    {
        public List<Standing> Rows { get; } = [];

        public Task<IReadOnlyList<Standing>> GetAcceptedAsync(
            Guid subjectId,
            string requesterUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Standing>>(Rows
                .Where(standing =>
                    standing.SubjectId == subjectId &&
                    standing.RequesterUserId == requesterUserId &&
                    standing.AcceptedAt is not null)
                .ToList());

        public Task<IReadOnlyList<Standing>> ListIssuedBySubjectAsync(
            Guid subjectId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Standing>>(
                Rows.Where(standing => standing.SubjectId == subjectId).ToList());

        public Task<IReadOnlyList<Standing>> ListHeldByAsync(
            string requesterUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Standing>>(
                Rows.Where(standing => standing.RequesterUserId == requesterUserId).ToList());

        public Task<Standing?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Rows.SingleOrDefault(standing => standing.Id == id));

        public Task AddAsync(Standing standing, CancellationToken cancellationToken)
        {
            Rows.Add(standing);

            return Task.CompletedTask;
        }

        // Standing is immutable, so the row is replaced.
        public Task<bool> AcceptAsync(
            Guid id,
            DateTimeOffset acceptedAt,
            CancellationToken cancellationToken)
        {
            var index = Rows.FindIndex(standing => standing.Id == id && standing.AcceptedAt is null);

            if (index < 0)
            {
                return Task.FromResult(false);
            }

            var pending = Rows[index];

            Rows[index] = new Standing
            {
                Id = pending.Id,
                SubjectId = pending.SubjectId,
                RequesterUserId = pending.RequesterUserId,
                Value = pending.Value,
                IssuerKind = pending.IssuerKind,
                Issuer = pending.Issuer,
                IssuedAt = pending.IssuedAt,
                AcceptedAt = acceptedAt,
            };

            return Task.FromResult(true);
        }
    }

    /// <summary>Only Sam has a profile; a holder does not need one.</summary>
    private sealed class Subjects : ISubjectRepository
    {
        private static readonly Subject Sam = new() { Id = SamSubjectId, UserId = SamUserId };

        public Task<Subject?> FindAsync(Guid subjectId, CancellationToken cancellationToken) =>
            Task.FromResult<Subject?>(subjectId == SamSubjectId ? Sam : null);

        public Task<Subject?> FindByUserIdAsync(string userId, CancellationToken cancellationToken) =>
            Task.FromResult<Subject?>(userId == SamUserId ? Sam : null);

        public Task AddAsync(Subject subject, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class SingleContextUserDirectory : IUserDirectory
    {
        private static readonly (string UserId, string Email)[] Accounts =
       [
           (SamUserId, SamEmail),
            (RiyaUserId, RiyaEmail),
        ];

        private int _inFlight;

        public async Task<string?> FindUserIdByEmailAsync(string email, CancellationToken cancellationToken)
        {
            try
            {
                await EnterAsync();

                return Accounts.FirstOrDefault(account => account.Email == email).UserId;
            }
            finally
            {
                Interlocked.Decrement(ref _inFlight);
            }
        }

        public async Task<string?> FindEmailAsync(string userId, CancellationToken cancellationToken)
        {
            try
            {
                await EnterAsync();

                return Accounts.FirstOrDefault(account => account.UserId == userId).Email;
            }
            finally
            {
                Interlocked.Decrement(ref _inFlight);
            }
        }

        private async Task EnterAsync()
        {
            if (Interlocked.Increment(ref _inFlight) > 1)
            {
                throw new InvalidOperationException("A second operation was started on this context.");
            }

            await Task.Yield();
        }
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
