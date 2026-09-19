using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Models;

namespace FacetIQ.Domain.Abstractions.Services;

/// <summary>Finds norms a proposed norm would tie with at request time.</summary>
public interface IConflictDetector
{
    /// <param name="existing">The subject's norms on the same claim key. Retired norms are ignored.</param>
    IReadOnlyList<NormConflict> Detect(Norm proposed, IReadOnlyList<Norm> existing);
}
