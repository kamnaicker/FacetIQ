using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;

namespace FacetIQ.Conformity.Tests.Gdpr;

/// <summary>Keeps written records in a list for assertions.</summary>
internal sealed class RecordingAuditRepository : IAuditRecordRepository
{
    public List<AuditRecord> Written { get; } = [];

    public Task AddAsync(AuditRecord record, CancellationToken cancellationToken)
    {
        Written.Add(record);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AuditRecord>> ListForSubjectAsync(
        Guid subjectId,
        int limit,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AuditRecord>>(
            Written
                .Where(record => record.SubjectId == subjectId)
                .OrderByDescending(record => record.Timestamp)
                .Take(limit)
                .ToList());

    public Task<IReadOnlyList<AuditRecord>> ListAllForSubjectAsync(Guid subjectId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<AuditRecord>>(
            Written
                .Where(record => record.SubjectId == subjectId)
                .OrderByDescending(record => record.Timestamp)
                .ToList());
    }
}
