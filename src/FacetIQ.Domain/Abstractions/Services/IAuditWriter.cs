using FacetIQ.Domain.Models;

namespace FacetIQ.Domain.Abstractions.Services;

/// <summary>Writes the audit record for one evaluated request.</summary>
public interface IAuditWriter
{
    Task RecordAsync(DisclosureRequest request, DisclosureResult result, CancellationToken cancellationToken);
}
