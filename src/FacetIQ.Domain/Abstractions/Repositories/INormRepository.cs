using FacetIQ.Domain.Entities;

namespace FacetIQ.Domain.Abstractions.Repositories;

public interface INormRepository
{
    /// <summary>
    /// The subject's current norms governing claims of the given key. Superseded revisions
    /// are excluded: only the rule in force can govern a new request.
    /// </summary>
    Task<IReadOnlyList<Norm>> GetGoverningAsync(
        Guid subjectId,
        string attributeKey,
        CancellationToken cancellationToken);
}
