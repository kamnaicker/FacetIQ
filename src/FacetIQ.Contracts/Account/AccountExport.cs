using FacetIQ.Contracts.Attributes;
using FacetIQ.Contracts.Disclosure;
using FacetIQ.Contracts.Norms;
using FacetIQ.Contracts.Standings;

namespace FacetIQ.Contracts.Account;

/// <summary>Everything the account can see about itself, in the shapes its pages already use.</summary>
public sealed record AccountExport
{
    public required string Email { get; init; }

    public required DateTimeOffset ExportedAt { get; init; }

    public required IReadOnlyList<AttributeResponse> Claims { get; init; }

    public required IReadOnlyList<NormResponse> Rules { get; init; }

    public required IReadOnlyList<StandingResponse> PeopleYouAdded { get; init; }

    public required IReadOnlyList<StandingResponse> PeopleWhoAddedYou { get; init; }

    /// <summary>Decisions about this account. Its own lookups of others belong to their history.</summary>
    public required IReadOnlyList<DisclosureRecordResponse> Requests { get; init; }
}
