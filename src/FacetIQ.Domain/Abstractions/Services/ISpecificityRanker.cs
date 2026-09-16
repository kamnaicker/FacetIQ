using FacetIQ.Domain.Models;

namespace FacetIQ.Domain.Abstractions.Services;

/// <summary>Picks the single most specific candidate, or reports no match or a tie.</summary>
public interface ISpecificityRanker
{
    NormSelection Select(IReadOnlyList<NormCandidate> candidates);
}
