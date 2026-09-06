using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;

namespace FacetIQ.Services.Tests;

/// <summary>
/// Keeps every record it is given so a test can assert on what the writer produced, including
/// what it deliberately left out.
/// </summary>
internal sealed class RecordingAuditRepository : IAuditRecordRepository
{
    public List<AuditRecord> Written { get; } = [];

    public Task AddAsync(AuditRecord record, CancellationToken cancellationToken)
    {
        Written.Add(record);

        return Task.CompletedTask;
    }
}
