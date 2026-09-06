using FacetIQ.Domain.Entities;

namespace FacetIQ.Domain.Abstractions.Repositories;

public interface INormRepository
{
    /// <summary>
    /// The subject's current norms governing claims of the given key. Superseded revisions
    /// are excluded: only the rule in force can govern a new request.
    /// </summary>
    Task<IReadOnlyList<Norm>> GetGoverningNormsAsync(
        Guid subjectId,
        string attributeKey,
        CancellationToken cancellationToken);

    /// <summary>
    /// Every norm the subject currently has in force, across all keys. Superseded revisions are
    /// excluded for the same reason as above: conflict detection compares a proposed norm against
    /// the rules that could actually govern a request, and a retired one cannot.
    /// </summary>
    Task<IReadOnlyList<Norm>> ListGoverningAsync(Guid subjectId, CancellationToken cancellationToken);

    /// <summary>
    /// Stores a newly authored norm. Detecting conflicts is the caller's job and happens first:
    /// a norm that collides is never offered here.
    /// </summary>
    Task AddAsync(Norm norm, CancellationToken cancellationToken);
}
