namespace FacetIQ.Domain.Enums;

/// <summary>Why a request was refused. Only RefusedByRule comes from a subject's rule; the rest are the engine's.</summary>
public enum DenyReasonCode
{
    NoMatchingNorm,
    AmbiguousNorms,
    RefusedByRule,
    PurposeIncompatible,
    ClaimUnavailable,
    TransformFailed
}
