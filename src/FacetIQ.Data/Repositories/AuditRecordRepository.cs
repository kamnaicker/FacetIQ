using FacetIQ.Data.Context;
using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;

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
}
