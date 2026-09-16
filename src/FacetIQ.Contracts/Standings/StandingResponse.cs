namespace FacetIQ.Contracts.Standings;

public sealed record StandingResponse
{
    public required Guid Id { get; init; }

    public required string Value { get; init; }

    public required string IssuerKind { get; init; }

    /// <summary>Institution name, or the issuing subject's email.</summary>
    public required string Issuer { get; init; }

    /// <summary>Holder's email. Null when the caller is the holder.</summary>
    public string? Holder { get; init; }

    public required DateTimeOffset IssuedAt { get; init; }

    /// <summary>Null while pending.</summary>
    public DateTimeOffset? AcceptedAt { get; init; }
}

/// <summary>Standings the caller issued, and standings issued to them.</summary>
public sealed record StandingsResponse
{
    public required IReadOnlyList<StandingResponse> Issued { get; init; }

    public required IReadOnlyList<StandingResponse> Held { get; init; }
}
