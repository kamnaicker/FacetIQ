using FacetIQ.Data.Context;
using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FacetIQ.Data.Repositories;

public sealed class AuditRecordRepository : IAuditRecordRepository
{
    private readonly FacetIQDbContext _context;

    public AuditRecordRepository(FacetIQDbContext context) => _context = context;

    public async Task AddAsync(AuditRecord record, CancellationToken cancellationToken)
    {
        _context.AuditRecords.Add(record);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditRecord>> ListForSubjectAsync(
        Guid subjectId,
        int limit,
        CancellationToken cancellationToken)
    {
        return await _context.AuditRecords
            .AsNoTracking()
            .Where(record => record.SubjectId == subjectId)
            .OrderByDescending(record => record.Timestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditRecord>> ListAllForSubjectAsync(
        Guid subjectId,
        CancellationToken cancellationToken)
    {
        return await _context.AuditRecords
            .AsNoTracking()
            .Where(record => record.SubjectId == subjectId)
            .OrderByDescending(record => record.Timestamp)
            .ToListAsync(cancellationToken);
    }
}
