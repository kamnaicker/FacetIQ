using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Models;

namespace FacetIQ.Domain.Abstractions.Services;

/// <summary>
/// Reduces a subject's norms to those that apply to a request, scoring each by specificity.
/// Pure: matching depends only on its arguments.
/// </summary>
public interface INormMatcher
{
    /// <param name="standings">
    /// The relationship terms the requester holds by accepted standing. Resolved by the caller,
    /// never taken from the request, which is why it arrives as a separate argument.
    /// </param>
    IReadOnlyList<NormCandidate> Match(
        IReadOnlyList<Norm> norms,
        DisclosureRequest request,
        IReadOnlySet<string> standings);
}
