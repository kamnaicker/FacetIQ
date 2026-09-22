using FacetIQ.Domain.Entities;

namespace FacetIQ.Domain.Abstractions.Repositories;

/// <summary>Append-only: no update or delete.</summary>
public interface IAuditRecordRepository
{
    Task AddAsync(AuditRecord record, CancellationToken cancellationToken);

    /// <summary>Newest first.</summary>
    Task<IReadOnlyList<AuditRecord>> ListForSubjectAsync(
        Guid subjectId,
        int limit,
        CancellationToken cancellationToken);

    /// <summary>Every record about the subject, newest first. For an export, where nothing may be left out.</summary>
    Task<IReadOnlyList<AuditRecord>> ListAllForSubjectAsync(Guid subjectId, CancellationToken cancellationToken);
}
