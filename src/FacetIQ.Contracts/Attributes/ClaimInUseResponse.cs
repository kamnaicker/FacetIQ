using FacetIQ.Contracts.Norms;

namespace FacetIQ.Contracts.Attributes;

/// <summary>409 body: the rules in force that still select the claim.</summary>
public sealed record ClaimInUseResponse
{
    public required IReadOnlyList<NormResponse> Rules { get; init; }
}
