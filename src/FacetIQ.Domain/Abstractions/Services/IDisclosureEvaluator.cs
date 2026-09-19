using FacetIQ.Domain.Models;

namespace FacetIQ.Domain.Abstractions.Services;

/// <summary>Match, rank, transform and audit one disclosure request.</summary>
public interface IDisclosureEvaluator
{
    Task<DisclosureResult> EvaluateAsync(DisclosureRequest request, CancellationToken cancellationToken);
}
