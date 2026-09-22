using FacetIQ.API.Mapping;
using FacetIQ.Contracts.Account;
using FacetIQ.Contracts.Disclosure;
using FacetIQ.Contracts.Standings;
using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;

namespace FacetIQ.API.Account;

/// <summary>Assembles what an account's pages show into one document the person can keep.</summary>
public sealed class AccountExporter
{
    private readonly ISubjectRepository _subjects;
    private readonly IAttributeRepository _attributes;
    private readonly INormRepository _norms;
    private readonly IStandingRepository _standings;
    private readonly IAuditRecordRepository _records;
    private readonly IUserDirectory _users;
    private readonly TimeProvider _clock;

    // Resolved once per export. Lookups run one at a time, as the DbContext cannot query concurrently.
    private readonly Dictionary<string, string?> _addresses = [];

    public AccountExporter(
        ISubjectRepository subjects,
        IAttributeRepository attributes,
        INormRepository norms,
        IStandingRepository standings,
        IAuditRecordRepository records,
        IUserDirectory users,
        TimeProvider clock)
    {
        _subjects = subjects;
        _attributes = attributes;
        _norms = norms;
        _standings = standings;
        _records = records;
        _users = users;
        _clock = clock;
    }

    /// <summary>Null when the account does not exist.</summary>
    public async Task<AccountExport?> ExportAsync(string userId, CancellationToken cancellationToken)
    {
        var email = await AddressOf(userId, cancellationToken);

        if (email is null)
        {
            return null;
        }

        var subject = await _subjects.FindByUserIdAsync(userId, cancellationToken);

        var claims = subject is null
            ? []
            : await _attributes.ListBySubjectAsync(subject.Id, cancellationToken);

        var rules = subject is null
            ? []
            : await _norms.ListGoverningAsync(subject.Id, cancellationToken);

        var issued = subject is null
            ? []
            : await _standings.ListIssuedBySubjectAsync(subject.Id, cancellationToken);

        var held = await _standings.ListHeldByAsync(userId, cancellationToken);

        var records = subject is null
            ? []
            : await _records.ListAllForSubjectAsync(subject.Id, cancellationToken);

        var peopleYouAdded = new List<StandingResponse>(issued.Count);
        foreach (var standing in issued)
        {
            peopleYouAdded.Add(StandingMapper.ToContract(
                standing,
                await IssuerOf(standing, cancellationToken),
                await AddressOf(standing.RequesterUserId, cancellationToken) ?? standing.RequesterUserId));
        }

        var peopleWhoAddedYou = new List<StandingResponse>(held.Count);
        foreach (var standing in held)
        {
            peopleWhoAddedYou.Add(StandingMapper.ToContract(standing, await IssuerOf(standing, cancellationToken), null));
        }

        var requests = new List<DisclosureRecordResponse>(records.Count);
        foreach (var record in records)
        {
            var requester = await AddressOf(record.RequesterUserId, cancellationToken);

            requests.Add(DisclosureRecordMapper.ToContract(record, requester, isSelf: record.RequesterUserId == userId));
        }

        return new AccountExport
        {
            Email = email,
            ExportedAt = _clock.GetUtcNow(),
            Claims = claims.Select(AttributeMapper.ToContract).ToList(),
            Rules = rules.Select(NormMapper.ToContract).ToList(),
            PeopleYouAdded = peopleYouAdded,
            PeopleWhoAddedYou = peopleWhoAddedYou,
            Requests = requests
        };
    }

    // An institution is named in the standing itself; a person is named by their address.
    private async Task<string> IssuerOf(Standing standing, CancellationToken cancellationToken)
    {
        if (standing.IssuerKind != IssuerKind.Subject)
        {
            return standing.Issuer;
        }

        return await AddressOf(standing.Issuer, cancellationToken) ?? standing.Issuer;
    }

    private async Task<string?> AddressOf(string userId, CancellationToken cancellationToken)
    {
        if (!_addresses.TryGetValue(userId, out var address))
        {
            address = await _users.FindEmailAsync(userId, cancellationToken);
            _addresses[userId] = address;
        }

        return address;
    }
}
