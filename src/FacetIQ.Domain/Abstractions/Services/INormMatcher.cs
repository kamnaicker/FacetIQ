using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Models;

namespace FacetIQ.Domain.Abstractions.Services;

/// <summary>
/// Reduces a subject's norms to those that apply to a request, scoring each by specificity.
/// Pure: matching depends only on its arguments.
/// </summary>
public interface INormMatcher
{
    IReadOnlyList<NormCandidate> Match(IReadOnlyList<Norm> norms, DisclosureRequest request);
}
