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
}
