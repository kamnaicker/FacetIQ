using FacetIQ.Domain.Entities;

namespace FacetIQ.Domain.Abstractions.Repositories;

public interface IAttributeRepository
{
    Task<SubjectAttribute?> FindAsync(Guid attributeId, CancellationToken cancellationToken);

    /// <summary>
    /// Every claim a subject holds under one key. A key identifies a kind of claim, not a
    /// single value, so this returns the set the subject is choosing between.
    /// </summary>
    Task<IReadOnlyList<SubjectAttribute>> ListByKeyAsync(
        Guid subjectId,
        string key,
        CancellationToken cancellationToken);

    /// <summary>
    /// Every claim a subject holds, across all keys. What the subject sees when reading back
    /// their own profile, where the whole set matters rather than one kind of claim.
    /// </summary>
    Task<IReadOnlyList<SubjectAttribute>> ListBySubjectAsync(
        Guid subjectId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Stores a claim the subject has authored. Nothing is derived from it and nothing replaces
    /// an existing claim: a new name is an addition to the set, not a correction of it.
    /// </summary>
    Task AddAsync(SubjectAttribute attribute, CancellationToken cancellationToken);
}
