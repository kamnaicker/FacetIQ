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
                candidates.Add(new NormCandidate(norm, Specificity(norm)));
            }
        }

        return candidates;
    }

    /// <summary>
    /// A null condition is a wildcard and admits any request. A bound condition must match
    /// exactly, so a norm either applies in full or not at all.
    ///
    /// A bound relationship is satisfied by holding it, not by claiming it: the test is
    /// membership of the resolved standings rather than equality with anything a caller sent.
    /// </summary>
    private static bool Applies(Norm norm, DisclosureRequest request, IReadOnlySet<string> standings) =>
        (norm.Relationship is null || standings.Contains(norm.Relationship)) &&
        (norm.Purpose is null || norm.Purpose == request.Purpose);

    /// <summary>
    /// Specificity counts a norm's bound conditions. Because a bound condition must match
    /// exactly for the norm to apply at all, the score does not vary with the request: it is
    /// a property of the norm alone, which is what allows ambiguity to be detected when a
    /// norm is authored rather than when a request arrives.
    /// </summary>
    private static int Specificity(Norm norm) =>
        (norm.Relationship is null ? 0 : 1) +
        (norm.Purpose is null ? 0 : 1);
}
