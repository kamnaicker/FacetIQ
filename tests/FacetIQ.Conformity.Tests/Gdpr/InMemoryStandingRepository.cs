using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;

namespace FacetIQ.Conformity.Tests.Gdpr;

/// <summary>Same accepted-only filter as StandingRepository, over a list.</summary>
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

    public Task<IReadOnlyList<Standing>> ListIssuedBySubjectAsync(
        Guid subjectId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<Standing>>(
            _standings.Where(standing => standing.SubjectId == subjectId).ToList());
    }

    public Task<IReadOnlyList<Standing>> ListHeldByAsync(
        string requesterUserId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<Standing>>(
            _standings.Where(standing => standing.RequesterUserId == requesterUserId).ToList());
    }

    // Neither the evaluator nor the export reads or changes a single standing.

    public Task<Standing?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task AddAsync(Standing standing, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<bool> AcceptAsync(
        Guid id,
        DateTimeOffset acceptedAt,
        CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }
}
