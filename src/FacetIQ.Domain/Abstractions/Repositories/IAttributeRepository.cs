using FacetIQ.Domain.Entities;

namespace FacetIQ.Domain.Abstractions.Repositories;

public interface IAttributeRepository
{
    Task<SubjectAttribute?> FindAsync(Guid attributeId, CancellationToken cancellationToken);

    /// <summary>All of the subject's claims under one key, e.g. every name they hold.</summary>
    Task<IReadOnlyList<SubjectAttribute>> ListByKeyAsync(
        Guid subjectId,
        string key,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SubjectAttribute>> ListBySubjectAsync(
        Guid subjectId,
        CancellationToken cancellationToken);

    Task AddAsync(SubjectAttribute attribute, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the claim and its retired norms. The caller must refuse first if a norm in force
    /// still selects it.
    /// </summary>
    Task<bool> DeleteAsync(Guid attributeId, CancellationToken cancellationToken);
}
