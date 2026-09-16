using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Models;

namespace FacetIQ.Domain.Abstractions.Services;

/// <summary>Filters norms to those that apply to the request and scores each.</summary>
public interface INormMatcher
{
    /// <param name="standings">Relationship terms from the requester's accepted standings.</param>
    IReadOnlyList<NormCandidate> Match(
        IReadOnlyList<Norm> norms,
        DisclosureRequest request,
        IReadOnlySet<string> standings);
}
