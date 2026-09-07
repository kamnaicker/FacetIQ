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
    /// Every norm the subject has in force, across all keys. Superseded revisions are excluded
    /// for the same reason as above.
    /// </summary>
    Task<IReadOnlyList<Norm>> ListGoverningAsync(Guid subjectId, CancellationToken cancellationToken);

    /// <summary>Detecting conflicts is the caller's job and happens first.</summary>
    Task AddAsync(Norm norm, CancellationToken cancellationToken);
}
