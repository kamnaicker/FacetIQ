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
}
