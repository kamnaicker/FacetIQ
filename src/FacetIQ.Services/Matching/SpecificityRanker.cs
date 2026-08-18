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

        // Two norms of equal specificity are not separated here. Contextual integrity gives
        // no flow parameter precedence over another, so there is no principled basis for
        // preferring the one that binds purpose over the one that binds relationship. The
        // tie is reported so the subject resolves it when authoring, rather than the engine
        // resolving it on their behalf.
        return leaders.Count == 1
            ? NormSelection.Of(leaders[0].Norm)
            : NormSelection.Ambiguous;
    }
}
