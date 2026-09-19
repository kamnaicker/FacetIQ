using FacetIQ.Domain.Enums;

namespace FacetIQ.Domain.Entities;

/// <summary>
/// One disclosure decision. The released value is not stored: the norm, transform and parameter
/// are enough to reproduce it. Ids are plain values with no foreign keys, so records survive
/// deletion of the subject, claim or norm.
/// </summary>
public sealed class AuditRecord
{
    public Guid Id { get; init; }

    public DateTimeOffset Timestamp { get; init; }

    public required string RequesterUserId { get; init; }

    public Guid SubjectId { get; init; }

    public required string RequestedAttributeKey { get; init; }

    public required Purpose Purpose { get; init; }

    public required RequestChannel Channel { get; init; }

    public required ActionType Outcome { get; init; }

    public DenyReasonCode? DenyReason { get; init; }

    public Guid? NormId { get; init; }

    public int? NormVersion { get; init; }

    public TransformKind? Transform { get; init; }

    public string? TransformParameter { get; init; }

    public string? JustifyingPrinciple { get; init; }
}
