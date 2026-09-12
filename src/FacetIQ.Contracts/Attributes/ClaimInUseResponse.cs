using FacetIQ.Contracts.Norms;

namespace FacetIQ.Contracts.Attributes;

/// <summary>Why a claim was not deleted: the rules in force that still release it.</summary>
public sealed record ClaimInUseResponse
{
    public required IReadOnlyList<NormResponse> Rules { get; init; }
}
