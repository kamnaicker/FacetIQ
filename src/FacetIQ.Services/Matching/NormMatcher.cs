using FacetIQ.Domain.Abstractions.Services;
using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Models;

namespace FacetIQ.Services.Matching;

public sealed class NormMatcher : INormMatcher
{
    public IReadOnlyList<NormCandidate> Match(IReadOnlyList<Norm> norms, DisclosureRequest request)
    {
        var candidates = new List<NormCandidate>();

        foreach (var norm in norms)
        {
            if (Applies(norm, request))
            {
                candidates.Add(new NormCandidate(norm, Specificity(norm)));
            }
        }

        return candidates;
    }

    /// <summary>
    /// A null condition is a wildcard and admits any request. A bound condition must match
    /// exactly, so a norm either applies in full or not at all.
    /// </summary>
    private static bool Applies(Norm norm, DisclosureRequest request) =>
        (norm.Relationship is null || Matches(norm.Relationship, request.Relationship)) &&
        (norm.Purpose is null || norm.Purpose == request.Purpose);

    private static bool Matches(string condition, string? value) =>
        string.Equals(condition, value, StringComparison.OrdinalIgnoreCase);

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
