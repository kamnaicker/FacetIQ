using FacetIQ.Domain.Enums;

namespace FacetIQ.Domain.Entities;

/// <summary>
/// An immutable record of one disclosure decision.
///
/// The disclosed value is deliberately absent. Recording the winning norm revision and the
/// transform applied is sufficient to reconstruct what was released, so a decision stays
/// verifiable without the system holding a second copy of the subject's data.
///
/// Audit records outlive the subject they describe, so identifiers are held as plain values
/// and no foreign keys are declared.
/// </summary>
public sealed class AuditRecord
{
    public Guid Id { get; init; }

    public DateTimeOffset Timestamp { get; init; }

    public required string RequesterUserId { get; init; }

    public Guid SubjectId { get; init; }

    /// <summary>The kind of claim requested, held by key rather than by reference.</summary>
    public required string RequestedAttributeKey { get; init; }

    public Purpose Purpose { get; init; }

    public RequestChannel Channel { get; init; }

    public ActionType Outcome { get; init; }

    public DenyReasonCode? DenyReason { get; init; }

    public Guid? NormId { get; init; }

    public int? NormVersion { get; init; }

    public TransformKind? Transform { get; init; }

    public string? TransformParameter { get; init; }

    /// <summary>Copied from the winning norm so the record carries its own justification.</summary>
    public string? JustifyingPrinciple { get; init; }
}
