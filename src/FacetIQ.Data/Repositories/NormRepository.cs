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
        // Also checks the claim's owner, so a norm can never select another subject's claim.
        return await _context.Norms
            .AsNoTracking()
            .Where(norm =>
                norm.SubjectId == subjectId &&
                norm.SupersededAt == null &&
                norm.Attribute.SubjectId == subjectId &&
                norm.Attribute.Key == attributeKey)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Norm>> ListGoverningAsync(
        Guid subjectId,
        CancellationToken cancellationToken)
    {
        return await _context.Norms
            .AsNoTracking()
            .Where(norm =>
                norm.SubjectId == subjectId &&
                norm.SupersededAt == null)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Norm norm, CancellationToken cancellationToken)
    {
        _context.Norms.Add(norm);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RetireAsync(Guid id, DateTimeOffset retiredAt, CancellationToken cancellationToken)
    {
        var retired = await _context.Norms
            .Where(norm => norm.Id == id && norm.SupersededAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(norm => norm.SupersededAt, retiredAt),
                cancellationToken);

        return retired > 0;
    }
}
