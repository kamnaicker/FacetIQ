using FacetIQ.Contracts.Standings;
using FacetIQ.Domain.Entities;

namespace FacetIQ.API.Mapping;

public static class StandingMapper
{
    /// <param name="issuer">The issuer's address for a person, or the institution's name.</param>
    /// <param name="holder">The holder's address, or null when the caller is the holder.</param>
    public static StandingResponse ToContract(Standing standing, string issuer, string? holder)
    {
        return new StandingResponse
        {
            Id = standing.Id,
            Value = standing.Value,
            IssuerKind = standing.IssuerKind.ToString(),
            Issuer = issuer,
            Holder = holder,
            IssuedAt = standing.IssuedAt,
            AcceptedAt = standing.AcceptedAt,
        };
    }
}
