using FacetIQ.Domain.Enums;

namespace FacetIQ.Domain.Entities;

/// <summary>
/// A relationship a requester holds towards a subject, issued by an institution or the subject.
/// The only source of relationship for matching. Issuers are recorded, not verified.
/// </summary>
public sealed class Standing
{
    public Guid Id { get; init; }

    public Guid SubjectId { get; init; }

    /// <summary>The holder's Identity user id.</summary>
    public required string RequesterUserId { get; init; }

    /// <summary>The relationship term norms match against, e.g. "colleague".</summary>
    public required string Value { get; init; }

    public IssuerKind IssuerKind { get; init; }

    /// <summary>An institution name, or the issuing subject's user id.</summary>
    public required string Issuer { get; init; }

    public DateTimeOffset IssuedAt { get; init; }

    /// <summary>Null until the holder accepts. Unaccepted standings are ignored by matching.</summary>
    public DateTimeOffset? AcceptedAt { get; init; }
}
