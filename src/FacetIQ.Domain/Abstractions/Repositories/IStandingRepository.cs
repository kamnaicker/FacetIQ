using FacetIQ.Domain.Entities;

namespace FacetIQ.Domain.Abstractions.Repositories;

public interface IStandingRepository
{
    /// <summary>
    /// The standings a requester holds towards a subject that have been accepted. Unaccepted
    /// rows are never returned, so an assertion nobody agreed to cannot reach a decision.
    /// </summary>
    Task<IReadOnlyList<Standing>> GetAcceptedAsync(
        Guid subjectId,
        string requesterUserId,
        CancellationToken cancellationToken);

    /// <summary>What a subject has issued about others, pending or accepted.</summary>
    Task<IReadOnlyList<Standing>> ListIssuedBySubjectAsync(
        Guid subjectId,
        CancellationToken cancellationToken);

    /// <summary>What has been issued to a requester, pending or accepted.</summary>
    Task<IReadOnlyList<Standing>> ListHeldByAsync(
        string requesterUserId,
        CancellationToken cancellationToken);

    Task<Standing?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(Standing standing, CancellationToken cancellationToken);

    /// <summary>
    /// Records acceptance. Returns false when the standing is gone or was already accepted, so a
    /// repeated accept cannot move the timestamp.
    /// </summary>
    Task<bool> AcceptAsync(Guid id, DateTimeOffset acceptedAt, CancellationToken cancellationToken);
}
