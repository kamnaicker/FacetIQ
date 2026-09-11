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

    // Single rather than First: the user identifier is uniquely indexed, so two matches is a
    // broken database, not a case to pick a winner from.
    public Task<Subject?> FindByUserIdAsync(string userId, CancellationToken cancellationToken) =>
        _context.Subjects
            .AsNoTracking()
            .SingleOrDefaultAsync(subject => subject.UserId == userId, cancellationToken);

    public async Task AddAsync(Subject subject, CancellationToken cancellationToken)
    {
        _context.Subjects.Add(subject);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
