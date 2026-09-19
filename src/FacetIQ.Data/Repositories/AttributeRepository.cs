using FacetIQ.Data.Context;
using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FacetIQ.Data.Repositories;

public sealed class AttributeRepository : IAttributeRepository
{
    private readonly FacetIQDbContext _context;

    public AttributeRepository(FacetIQDbContext context) => _context = context;

    public Task<SubjectAttribute?> FindAsync(Guid attributeId, CancellationToken cancellationToken) =>
        _context.SubjectAttributes
            .AsNoTracking()
            .SingleOrDefaultAsync(attribute => attribute.Id == attributeId, cancellationToken);

    public async Task<IReadOnlyList<SubjectAttribute>> ListByKeyAsync(
        Guid subjectId,
        string key,
        CancellationToken cancellationToken)
    {
        return await _context.SubjectAttributes
            .AsNoTracking()
            .Where(attribute => attribute.SubjectId == subjectId && attribute.Key == key)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SubjectAttribute>> ListBySubjectAsync(
        Guid subjectId,
        CancellationToken cancellationToken)
    {
        return await _context.SubjectAttributes
            .AsNoTracking()
            .Where(attribute => attribute.SubjectId == subjectId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(SubjectAttribute attribute, CancellationToken cancellationToken)
    {
        _context.SubjectAttributes.Add(attribute);

        await _context.SaveChangesAsync(cancellationToken);
    }

    // Retired norms would block the delete through their foreign key. Audit rows keep their own
    // copy of what the norm decided, so nothing is lost.
    public async Task<bool> DeleteAsync(Guid attributeId, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        await _context.Norms
            .Where(norm => norm.AttributeId == attributeId && norm.SupersededAt != null)
            .ExecuteDeleteAsync(cancellationToken);

        var deleted = await _context.SubjectAttributes
            .Where(attribute => attribute.Id == attributeId)
            .ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return deleted > 0;
    }
}
