using FacetIQ.Domain.Enums;

namespace FacetIQ.Domain.Entities;

/// <summary>
/// A subject's rule: when a request matches the conditions, disclose this claim in this form.
/// Null conditions are wildcards. Norms are never edited; removing one sets SupersededAt.
/// </summary>
public sealed class Norm
{
    public Guid Id { get; init; }

    /// <summary>Part of the key and recorded on audit rows. Always 1, as there is no edit path yet.</summary>
    public int Version { get; init; }

    /// <summary>Set when retired. A retired norm governs nothing and is deleted with its claim.</summary>
    public DateTimeOffset? SupersededAt { get; init; }

    public Guid SubjectId { get; init; }

    public Guid AttributeId { get; init; }

    public SubjectAttribute Attribute { get; init; } = null!;

    public string? Relationship { get; init; }

    public Purpose? Purpose { get; init; }

    public ActionType Action { get; init; }

    public TransformKind Transform { get; init; }

    public string? TransformParameter { get; init; }

    /// <summary>Set on a refusal. The engine always records it as RefusedByRule.</summary>
    public DenyReasonCode? DenyReason { get; init; }

    /// <summary>Copied onto each audit record this norm decides.</summary>
    public required string JustifyingPrinciple { get; init; }
}
