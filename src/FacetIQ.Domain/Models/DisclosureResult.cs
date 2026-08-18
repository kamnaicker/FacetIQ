using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;

namespace FacetIQ.Domain.Models;

/// <summary>
/// The outcome of an evaluation. The winning norm travels with the result so the audit
/// writer can record which revision governed the decision without re-reading it.
/// </summary>
public sealed record DisclosureResult
{
    public required ActionType Outcome { get; init; }

    public string? Value { get; init; }

    public DenyReasonCode? DenyReason { get; init; }

    public Norm? Norm { get; init; }

    public static DisclosureResult Denied(DenyReasonCode reason, Norm? norm = null) => new()
    {
        Outcome = ActionType.Deny,
        DenyReason = reason,
        Norm = norm
    };

    /// <summary>
    /// Return and Transform differ only in whether the released value was shaped, so the
    /// distinction is derived here rather than being set independently by callers.
    /// </summary>
    public static DisclosureResult Disclosed(Norm norm, string value) => new()
    {
        Outcome = norm.Transform == TransformKind.None ? ActionType.Return : ActionType.Transform,
        Value = value,
        Norm = norm
    };
}
