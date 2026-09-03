using FacetIQ.Data.Context;
using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FacetIQ.Data.Repositories;

public sealed class NormRepository : INormRepository
{
    private readonly FacetIQDbContext _context;

    public NormRepository(FacetIQDbContext context) => _context = context;

    public async Task<IReadOnlyList<Norm>> GetGoverningNormsAsync(
        Guid subjectId,
        string attributeKey,
        CancellationToken cancellationToken)
    {
        // Joining through the attribute keeps the key filter in the database: a subject may
        // hold many norms, and only those pointing at claims of this kind can apply.
        return await _context.Norms
            .AsNoTracking()
            .Where(norm =>
                norm.SubjectId == subjectId &&
                norm.SupersededAt == null &&
                norm.Attribute.Key == attributeKey)
            .ToListAsync(cancellationToken);
    }
}
