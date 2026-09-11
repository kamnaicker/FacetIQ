using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;

namespace FacetIQ.Conformity.Tests.Gdpr;

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

    // The evaluator is the only thing under test here, and it reads accepted standings only.
    public Task<IReadOnlyList<Standing>> ListIssuedBySubjectAsync(
        Guid subjectId,
        CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<IReadOnlyList<Standing>> ListHeldByAsync(
        string requesterUserId,
        CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Standing?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task AddAsync(Standing standing, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<bool> AcceptAsync(
        Guid id,
        DateTimeOffset acceptedAt,
        CancellationToken cancellationToken) => throw new NotSupportedException();
}
