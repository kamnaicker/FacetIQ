using FacetIQ.Domain.Entities;

namespace FacetIQ.Domain.Abstractions.Repositories;

public interface INormRepository
{
    /// <summary>The subject's norms in force on claims with this key.</summary>
    Task<IReadOnlyList<Norm>> GetGoverningNormsAsync(
        Guid subjectId,
        string attributeKey,
        CancellationToken cancellationToken);

    /// <summary>The subject's norms in force across all keys.</summary>
    Task<IReadOnlyList<Norm>> ListGoverningAsync(Guid subjectId, CancellationToken cancellationToken);

    /// <summary>Does not check for conflicts; the caller does that first.</summary>
    Task AddAsync(Norm norm, CancellationToken cancellationToken);

    /// <summary>Sets SupersededAt. Returns false if the norm was not in force.</summary>
    Task<bool> RetireAsync(Guid id, DateTimeOffset retiredAt, CancellationToken cancellationToken);
}
