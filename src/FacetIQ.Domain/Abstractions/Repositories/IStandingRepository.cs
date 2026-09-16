using FacetIQ.Domain.Entities;

namespace FacetIQ.Domain.Abstractions.Repositories;

public interface IStandingRepository
{
    /// <summary>Accepted standings the requester holds towards the subject. Pending ones are excluded.</summary>
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

    /// <summary>Returns false if missing or already accepted; the first timestamp is kept.</summary>
    Task<bool> AcceptAsync(Guid id, DateTimeOffset acceptedAt, CancellationToken cancellationToken);
}
