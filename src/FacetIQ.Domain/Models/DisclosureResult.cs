using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;

namespace FacetIQ.Domain.Models;

/// <summary>An evaluation outcome. Carries the deciding norm so the audit writer can record it.</summary>
public sealed record DisclosureResult
{
    public required ActionType Outcome { get; init; }

    public string? Value { get; init; }

    /// <summary>Self-access only: every claim under the key.</summary>
    public IReadOnlyList<string>? Values { get; init; }

    public DenyReasonCode? DenyReason { get; init; }

    public Norm? Norm { get; init; }

    public static DisclosureResult Denied(DenyReasonCode reason, Norm? norm = null) => new()
    {
        Outcome = ActionType.Deny,
        DenyReason = reason,
        Norm = norm
    };

    // Outcome is derived from the norm so callers cannot set Return and Transform inconsistently.
    public static DisclosureResult Disclosed(Norm norm, string value) => new()
    {
        Outcome = norm.Transform == TransformKind.None ? ActionType.Return : ActionType.Transform,
        Value = value,
        Norm = norm
    };

    public static DisclosureResult SelfAccess(IReadOnlyList<string> values) => new()
    {
        Outcome = ActionType.Return,
        Values = values
    };
}
