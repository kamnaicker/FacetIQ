using System.Security.Claims;
using FacetIQ.API.Controllers;
using FacetIQ.Contracts.Disclosure;
using FacetIQ.Data.Identity;
using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FacetIQ.Conformity.Tests.Gdpr;

/// <summary>O5, O6: a subject reads only their own audit records.</summary>
public class HistoryControllerTests
{
    private const string OwnerUserId = "owner-1";
    private const string AskerUserId = "asker-1";

    private static readonly Guid SubjectId = new("0a5f4d8e-0000-4000-8000-000000000001");
    private static readonly Guid OtherSubjectId = new("0a5f4d8e-0000-4000-8000-000000000002");

    [Fact]
    public async Task History_ListsOnlyTheCallersOwnRecords_NewestFirst()
    {
        var audit = new RecordingAuditRepository();
        audit.Written.Add(Record(SubjectId, ActionType.Return, DateTimeOffset.UnixEpoch));
        audit.Written.Add(Record(OtherSubjectId, ActionType.Return, DateTimeOffset.UnixEpoch.AddHours(1)));
        audit.Written.Add(Record(SubjectId, ActionType.Deny, DateTimeOffset.UnixEpoch.AddHours(2)));

        var response = await ControllerFor(audit, Owner()).Get(CancellationToken.None);

        var listed = Assert.IsType<List<DisclosureRecordResponse>>(
            Assert.IsType<OkObjectResult>(response.Result).Value);

        Assert.Equal(2, listed.Count);
        Assert.Equal("Deny", listed[0].Outcome);
        Assert.Equal("Return", listed[1].Outcome);
        Assert.All(listed, entry => Assert.Equal("asker-1@example.test", entry.Requester));
    }

    [Fact]
    public async Task AccountOwningNoSubject_HasNoHistory()
    {
        var audit = new RecordingAuditRepository();
        audit.Written.Add(Record(SubjectId, ActionType.Return, DateTimeOffset.UnixEpoch));

        var stranger = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "stranger-1")], "test"));

        var response = await ControllerFor(audit, stranger).Get(CancellationToken.None);

        Assert.IsType<ForbidResult>(response.Result);
    }

    private static HistoryController ControllerFor(RecordingAuditRepository audit, ClaimsPrincipal caller) =>
        new(
            new InMemorySubjectRepository(new Subject { Id = SubjectId, UserId = OwnerUserId }),
            audit,
            new AccountsByIdentifier())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = caller }
            }
        };

    private static ClaimsPrincipal Owner() =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, OwnerUserId)], "test"));

    private static AuditRecord Record(Guid subjectId, ActionType outcome, DateTimeOffset at) => new()
    {
        Id = Guid.NewGuid(),
        Timestamp = at,
        RequesterUserId = AskerUserId,
        SubjectId = subjectId,
        RequestedAttributeKey = "name",
        Purpose = Purpose.Social,
        Channel = RequestChannel.Api,
        Outcome = outcome,
        DenyReason = outcome == ActionType.Deny ? DenyReasonCode.NoMatchingNorm : null
    };

    /// <summary>Resolves any id to "{id}@example.test".</summary>
    private sealed class AccountsByIdentifier()
        : UserManager<AppUser>(new EmptyUserStore(), null!, null!, null!, null!, null!, null!, null!, null!)
    {
        public override Task<AppUser?> FindByIdAsync(string userId) =>
            Task.FromResult<AppUser?>(new AppUser { Id = userId, Email = $"{userId}@example.test" });
    }
}
