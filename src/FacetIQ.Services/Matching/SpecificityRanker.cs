using FacetIQ.Domain.Abstractions.Services;
using FacetIQ.Domain.Models;

namespace FacetIQ.Services.Matching;

public sealed class SpecificityRanker : ISpecificityRanker
{
    public NormSelection Select(IReadOnlyList<NormCandidate> candidates)
    {
        if (candidates.Count == 0)
        {
            return NormSelection.NoMatch;
        }

        var highest = candidates.Max(candidate => candidate.Specificity);
        var leaders = candidates.Where(candidate => candidate.Specificity == highest).ToList();

        // Ties are refused, not broken: neither purpose nor relationship takes precedence.
        return leaders.Count == 1
            ? NormSelection.Of(leaders[0].Norm)
            : NormSelection.Ambiguous;
    }
}
