namespace FacetIQ.Domain.Enums;

/// <summary>
/// Why a request was refused. RefusedByRule is a refusal the subject authored; PurposeIncompatible
/// is the engine overriding a permission because the claim was collected for another purpose.
/// Kept distinct so an audit record says which.
/// </summary>
public enum DenyReasonCode
{
    NoMatchingNorm,
    AmbiguousNorms,
    RefusedByRule,
    PurposeIncompatible,
    ClaimUnavailable
}
