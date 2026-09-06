using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;

namespace FacetIQ.Services.Tests;

/// <summary>
/// Returns the standings it was given, already accepted. Tests that need to prove acceptance
/// is load-bearing construct unaccepted rows themselves and assert they are filtered out.
/// </summary>
internal sealed class InMemoryStandingRepository : IStandingRepository
{
    private readonly IReadOnlyList<Standing> _standings;

    public InMemoryStandingRepository(params Standing[] standings) => _standings = standings;

    public Task<IReadOnlyList<Standing>> GetAcceptedAsync(
        Guid subjectId,
        string requesterUserId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Standing>>(
            _standings
                .Where(standing =>
                    standing.SubjectId == subjectId &&
                    standing.RequesterUserId == requesterUserId &&
                    standing.AcceptedAt is not null)
                .ToList());
}
