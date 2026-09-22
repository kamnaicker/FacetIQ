using FacetIQ.API.Account;
using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;

namespace FacetIQ.Conformity.Tests.Gdpr;

/// <summary>
/// Access and portability: the export holds everything the account can see about itself, and
/// nothing that belongs to someone else's history.
/// </summary>
public class AccountExportTests
{
    private const string OwnerUserId = "owner-1";
    private const string OtherUserId = "other-1";
    private const string GoneUserId = "gone-1";

    private static readonly Guid SubjectId = new("0a5f4d8e-0000-4000-8000-000000000011");
    private static readonly Guid OtherSubjectId = new("0a5f4d8e-0000-4000-8000-000000000012");

    [Fact]
    public async Task Export_HoldsTheAccountsClaimsRulesAndPeople()
    {
        var export = await Exporter().ExportAsync(OwnerUserId, CancellationToken.None);

        Assert.NotNull(export);
        Assert.Equal("owner@example.test", export.Email);
        Assert.Equal("Amara Nwosu", Assert.Single(export.Claims).Value);
        Assert.Single(export.Rules);

        var added = Assert.Single(export.PeopleYouAdded);
        Assert.Equal("other@example.test", added.Holder);
        Assert.Equal("colleague", added.Value);

        var addedYou = Assert.Single(export.PeopleWhoAddedYou);
        Assert.Equal("other@example.test", addedYou.Issuer);
        Assert.Equal("friend", addedYou.Value);
    }

    [Fact]
    public async Task Export_HoldsDecisionsAboutTheAccountButNotItsLookupsOfOthers()
    {
        var export = await Exporter().ExportAsync(OwnerUserId, CancellationToken.None);

        Assert.NotNull(export);
        Assert.Equal(2, export.Requests.Count);
        Assert.DoesNotContain(export.Requests, request => request.IsSelf);
    }

    [Fact]
    public async Task RequesterWhoseAccountIsGone_IsNamedByNobody()
    {
        var export = await Exporter().ExportAsync(OwnerUserId, CancellationToken.None);

        Assert.NotNull(export);
        Assert.Contains(export.Requests, request => request.Requester is null);
        Assert.Contains(export.Requests, request => request.Requester == "other@example.test");
    }

    [Fact]
    public async Task UnknownAccount_HasNoExport()
    {
        var export = await Exporter().ExportAsync("no-such-user", CancellationToken.None);

        Assert.Null(export);
    }

    private static AccountExporter Exporter()
    {
        var ownClaim = Claim(SubjectId, "Amara Nwosu");
        var otherClaim = Claim(OtherSubjectId, "Someone Else");

        var audit = new RecordingAuditRepository();
        audit.Written.Add(Record(SubjectId, OtherUserId));
        audit.Written.Add(Record(SubjectId, GoneUserId));
        audit.Written.Add(Record(OtherSubjectId, OwnerUserId));

        return new AccountExporter(
            new InMemorySubjectRepository(new Subject { Id = SubjectId, UserId = OwnerUserId }),
            new InMemoryAttributeRepository(ownClaim, otherClaim),
            new InMemoryNormRepository(Rule(ownClaim)),
            new InMemoryStandingRepository(
                Standing(SubjectId, OtherUserId, OwnerUserId, "colleague"),
                Standing(OtherSubjectId, OwnerUserId, OtherUserId, "friend")),
            audit,
            new InMemoryUserDirectory(
                (OwnerUserId, "owner@example.test"),
                (OtherUserId, "other@example.test")),
            TimeProvider.System);
    }

    private static SubjectAttribute Claim(Guid subjectId, string value)
    {
        return new SubjectAttribute
        {
            Id = Guid.NewGuid(),
            SubjectId = subjectId,
            Key = "name",
            Value = value,
            Label = "legal"
        };
    }

    private static Norm Rule(SubjectAttribute claim)
    {
        return new Norm
        {
            Id = Guid.NewGuid(),
            Version = 1,
            SubjectId = claim.SubjectId,
            AttributeId = claim.Id,
            Attribute = claim,
            Action = ActionType.Return,
            Transform = TransformKind.None,
            JustifyingPrinciple = "Test rule."
        };
    }

    private static Standing Standing(Guid issuerSubjectId, string holderUserId, string issuerUserId, string value)
    {
        return new Standing
        {
            Id = Guid.NewGuid(),
            SubjectId = issuerSubjectId,
            RequesterUserId = holderUserId,
            Value = value,
            IssuerKind = IssuerKind.Subject,
            Issuer = issuerUserId,
            IssuedAt = DateTimeOffset.UnixEpoch,
            AcceptedAt = DateTimeOffset.UnixEpoch
        };
    }

    private static AuditRecord Record(Guid subjectId, string requesterUserId)
    {
        return new AuditRecord
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UnixEpoch,
            RequesterUserId = requesterUserId,
            SubjectId = subjectId,
            RequestedAttributeKey = "name",
            Purpose = Purpose.Social,
            Channel = RequestChannel.Api,
            Outcome = ActionType.Return
        };
    }
}
