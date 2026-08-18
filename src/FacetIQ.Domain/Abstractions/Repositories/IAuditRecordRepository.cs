using FacetIQ.Domain.Entities;

namespace FacetIQ.Domain.Abstractions.Repositories;

/// <summary>
/// Append-only. There is deliberately no update or delete: an audit record is a statement
/// about something that happened, and it outlives the subject it describes.
/// </summary>
public interface IAuditRecordRepository
{
    Task AddAsync(AuditRecord record, CancellationToken cancellationToken);
}
