using FacetIQ.Data.Context;
using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FacetIQ.Data.Repositories;

public sealed class StandingRepository : IStandingRepository
{
    private readonly FacetIQDbContext _context;

    public StandingRepository(FacetIQDbContext context) => _context = context;

    public async Task<IReadOnlyList<Standing>> GetAcceptedAsync(
        Guid subjectId,
        string requesterUserId,
        CancellationToken cancellationToken)
    {
        return await _context.Standings
            .AsNoTracking()
            .Where(standing =>
                standing.SubjectId == subjectId &&
                standing.RequesterUserId == requesterUserId &&
                standing.AcceptedAt != null)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Standing>> ListIssuedBySubjectAsync(
        Guid subjectId,
        CancellationToken cancellationToken)
    {
        return await _context.Standings
            .AsNoTracking()
            .Where(standing => standing.SubjectId == subjectId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Standing>> ListHeldByAsync(
        string requesterUserId,
        CancellationToken cancellationToken)
    {
        return await _context.Standings
            .AsNoTracking()
            .Where(standing => standing.RequesterUserId == requesterUserId)
            .ToListAsync(cancellationToken);
    }

    public Task<Standing?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Standings
            .AsNoTracking()
            .SingleOrDefaultAsync(standing => standing.Id == id, cancellationToken);

    public async Task AddAsync(Standing standing, CancellationToken cancellationToken)
    {
        _context.Standings.Add(standing);

        await _context.SaveChangesAsync(cancellationToken);
    }

    // ExecuteUpdate because the entity is immutable. The null check keeps the first timestamp.
    public async Task<bool> AcceptAsync(
        Guid id,
        DateTimeOffset acceptedAt,
        CancellationToken cancellationToken)
    {
        var updated = await _context.Standings
            .Where(standing => standing.Id == id && standing.AcceptedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(standing => standing.AcceptedAt, acceptedAt),
                cancellationToken);

        return updated > 0;
    }
}
