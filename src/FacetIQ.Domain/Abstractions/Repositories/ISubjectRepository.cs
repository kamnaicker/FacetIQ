using FacetIQ.Domain.Entities;

namespace FacetIQ.Domain.Abstractions.Repositories;

public interface ISubjectRepository
{
    Task<Subject?> FindAsync(Guid subjectId, CancellationToken cancellationToken);

    /// <summary>The subject owned by this account, or null.</summary>
    Task<Subject?> FindByUserIdAsync(string userId, CancellationToken cancellationToken);

    /// <summary>Stores the subject, or returns the one another request stored first.</summary>
    Task<Subject> AddOrGetAsync(Subject subject, CancellationToken cancellationToken);
}
