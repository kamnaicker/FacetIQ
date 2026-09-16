using FacetIQ.Domain.Abstractions.Services;
using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Models;

namespace FacetIQ.Services.Matching;

public sealed class NormMatcher : INormMatcher
{
    public IReadOnlyList<NormCandidate> Match(
        IReadOnlyList<Norm> norms,
        DisclosureRequest request,
        IReadOnlySet<string> standings)
    {
        var candidates = new List<NormCandidate>();

        foreach (var norm in norms)
        {
            if (Applies(norm, request, standings))
            {
                candidates.Add(new NormCandidate(norm, NormSpecificity.Of(norm)));
            }
        }

        return candidates;
    }

    // Null matches anything. A bound relationship must be among the requester's accepted standings.
    private static bool Applies(Norm norm, DisclosureRequest request, IReadOnlySet<string> standings) =>
        (norm.Relationship is null || standings.Contains(norm.Relationship)) &&
        (norm.Purpose is null || norm.Purpose == request.Purpose);
}
