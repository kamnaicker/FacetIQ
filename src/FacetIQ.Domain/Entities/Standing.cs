using FacetIQ.Domain.Enums;

namespace FacetIQ.Domain.Entities;

/// <summary>
/// A relationship a requester holds towards a subject, asserted by someone other than the
/// requester. This is the only thing that can place a caller in a context a norm was written
/// about, because relationship is never read from a request.
///
/// An institution issuing a credential covers a requester the subject has never met. A subject
/// issuing one covers a person they know, and the acceptance is what stops either party
/// asserting a relationship about the other unilaterally.
///
/// The system does not verify that an issuer is who it claims to be. It records that a decision
/// was made on this assertion, from this issuer, at this time.
/// </summary>
public sealed class Standing
{
    public Guid Id { get; init; }

    /// <summary>The subject this standing is held towards.</summary>
    public Guid SubjectId { get; init; }

    /// <summary>The requester holding it, matching the authenticated principal.</summary>
    public required string RequesterUserId { get; init; }

    /// <summary>The relationship term a norm is matched against, for example "colleague".</summary>
    public required string Value { get; init; }

    public IssuerKind IssuerKind { get; init; }

    /// <summary>
    /// Who asserted it, held as a plain value. The domain names no institutional register and
    /// no identity store, which is the boundary this project deliberately does not cross.
    /// </summary>
    public required string Issuer { get; init; }

    public DateTimeOffset IssuedAt { get; init; }

    /// <summary>
    /// Null until accepted. Only accepted standings reach matching, so an assertion nobody
    /// agreed to changes no decision.
    /// </summary>
    public DateTimeOffset? AcceptedAt { get; init; }
}
