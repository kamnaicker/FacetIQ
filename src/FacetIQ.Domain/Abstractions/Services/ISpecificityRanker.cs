using FacetIQ.Domain.Models;

namespace FacetIQ.Domain.Abstractions.Services;

/// <summary>
/// Chooses the governing norm from a set of candidates, or reports that no single norm
/// governs. Pure: ranking depends only on its arguments.
/// </summary>
public interface ISpecificityRanker
{
    NormSelection Select(IReadOnlyList<NormCandidate> candidates);
}
