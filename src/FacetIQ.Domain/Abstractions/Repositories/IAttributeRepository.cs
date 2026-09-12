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
    /// Every claim a subject holds, across all keys. What the subject sees reading back their own
    /// profile.
    /// </summary>
    Task<IReadOnlyList<SubjectAttribute>> ListBySubjectAsync(
        Guid subjectId,
        CancellationToken cancellationToken);

    /// <summary>Nothing replaces an existing claim: a new name is an addition to the set.</summary>
    Task AddAsync(SubjectAttribute attribute, CancellationToken cancellationToken);

    /// <summary>
    /// Erases a claim along with any retired rules about it. The caller refuses first if a rule in
    /// force still releases it.
    /// </summary>
    Task<bool> DeleteAsync(Guid attributeId, CancellationToken cancellationToken);
}
