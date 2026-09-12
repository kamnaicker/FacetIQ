namespace FacetIQ.Contracts.Standings;

public sealed record StandingResponse
{
    public required Guid Id { get; init; }

    public required string Value { get; init; }

    public required string IssuerKind { get; init; }

    /// <summary>Readable rather than an identifier: an institution's name, or the issuer's address.</summary>
    public required string Issuer { get; init; }

    /// <summary>Who holds it. Null when the caller is the holder rather than the issuer.</summary>
    public string? Holder { get; init; }

    public required DateTimeOffset IssuedAt { get; init; }

    /// <summary>Null while pending. Only accepted standings affect a decision.</summary>
    public DateTimeOffset? AcceptedAt { get; init; }
}

/// <summary>Both directions, since a person both issues standings and holds them.</summary>
public sealed record StandingsResponse
{
    public required IReadOnlyList<StandingResponse> Issued { get; init; }

    public required IReadOnlyList<StandingResponse> Held { get; init; }
}
