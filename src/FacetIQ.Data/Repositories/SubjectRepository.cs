using FacetIQ.Data.Context;
using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FacetIQ.Data.Repositories;

public sealed class SubjectRepository : ISubjectRepository
{
    private readonly FacetIQDbContext _context;

    public SubjectRepository(FacetIQDbContext context) => _context = context;

    public Task<Subject?> FindAsync(Guid subjectId, CancellationToken cancellationToken) =>
        _context.Subjects
            .AsNoTracking()
            .SingleOrDefaultAsync(subject => subject.Id == subjectId, cancellationToken);

    // Single: UserId is unique, so two matches means corrupt data and should throw.
    public Task<Subject?> FindByUserIdAsync(string userId, CancellationToken cancellationToken) =>
        _context.Subjects
            .AsNoTracking()
            .SingleOrDefaultAsync(subject => subject.UserId == userId, cancellationToken);

    public async Task<Subject> AddOrGetAsync(Subject subject, CancellationToken cancellationToken)
    {
        var entry = _context.Subjects.Add(subject);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);

            return subject;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Another request stored one first. Drop this insert and use the row that won.
            entry.State = EntityState.Detached;

            return await FindByUserIdAsync(subject.UserId, cancellationToken)
                ?? throw new InvalidOperationException("The subject that caused the conflict could not be read.");
        }
    }
}
