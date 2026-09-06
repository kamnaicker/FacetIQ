using FacetIQ.Domain.Models;

namespace FacetIQ.Domain.Abstractions.Services;

/// <summary>
/// Records a decision. Every evaluated request produces exactly one record, whatever the
/// outcome, so a refusal is as traceable as a disclosure.
/// </summary>
public interface IAuditWriter
{
    Task RecordAsync(DisclosureRequest request, DisclosureResult result, CancellationToken cancellationToken);
}
