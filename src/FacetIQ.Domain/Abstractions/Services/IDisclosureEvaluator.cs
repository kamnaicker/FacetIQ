using FacetIQ.Domain.Models;

namespace FacetIQ.Domain.Abstractions.Services;

/// <summary>
/// Evaluates a disclosure request end to end: select the governing norm, resolve the claim
/// it names, shape the value, and record the decision.
/// </summary>
public interface IDisclosureEvaluator
{
    Task<DisclosureResult> EvaluateAsync(DisclosureRequest request, CancellationToken cancellationToken);
}
