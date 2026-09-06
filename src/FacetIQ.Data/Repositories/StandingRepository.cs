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
}
