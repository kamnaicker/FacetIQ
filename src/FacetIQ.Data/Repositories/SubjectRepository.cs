using FacetIQ.Data.Context;
using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FacetIQ.Data.Repositories;

public sealed class SubjectRepository : ISubjectRepository
{
    private readonly FacetIQDbContext _context;

    public SubjectRepository(FacetIQDbContext context) => _context = context;

    public Task<Subject?> FindAsync(Guid subjectId, CancellationToken cancellationToken) =>
        _context.Subjects
            .AsNoTracking()
            .SingleOrDefaultAsync(subject => subject.Id == subjectId, cancellationToken);
}
