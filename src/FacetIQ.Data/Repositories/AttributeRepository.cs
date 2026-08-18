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
}
