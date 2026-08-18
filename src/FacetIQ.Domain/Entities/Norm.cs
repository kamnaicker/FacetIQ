using FacetIQ.Domain.Enums;

namespace FacetIQ.Domain.Entities;

/// <summary>
/// A rule authored by a subject: under these conditions, disclose this claim in this form.
///
/// The three conditions describe an information flow. Each is nullable, and null is a
/// wildcard matching any request. A norm is never edited in place; a change creates a new
/// revision, so an audit record always names the rule that was actually in force.
/// </summary>
public sealed class Norm
{
    public Guid Id { get; init; }

    public int Version { get; init; }

    /// <summary>
    /// Set when a later revision replaces this one. A superseded norm can never govern a new
    /// request, but it must remain readable so audit records that name it stay meaningful.
    /// </summary>
    public DateTimeOffset? SupersededAt { get; init; }

    public Guid SubjectId { get; init; }

    /// <summary>The claim this norm selects. Selection precedes any transformation.</summary>
    public Guid AttributeId { get; init; }

    public SubjectAttribute Attribute { get; init; } = null!;

    public string? Relationship { get; init; }

    public Purpose? Purpose { get; init; }

    public RequestChannel? Channel { get; init; }

    public ActionType Action { get; init; }

    public TransformKind Transform { get; init; }

    /// <summary>Argument to the transform, where one applies. Interpreted by the transform.</summary>
    public string? TransformParameter { get; init; }

    /// <summary>Set when this norm authors an explicit refusal rather than a disclosure.</summary>
    public DenyReasonCode? DenyReason { get; init; }

    /// <summary>
    /// The principle the subject is invoking, carried into the audit record so a decision
    /// can be traced back to the rule that justifies it.
    /// </summary>
    public required string JustifyingPrinciple { get; init; }
}
