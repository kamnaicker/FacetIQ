using FacetIQ.Domain.Entities;

namespace FacetIQ.Domain.Abstractions.Repositories;

public interface ISubjectRepository
{
    Task<Subject?> FindAsync(Guid subjectId, CancellationToken cancellationToken);

    /// <summary>
    /// The subject an account owns, or null where none is bound to it. Authoring resolves the
    /// subject this way rather than from the route or the body, so a caller can only ever write
    /// into their own profile.
    /// </summary>
    Task<Subject?> FindByUserIdAsync(string userId, CancellationToken cancellationToken);
}
