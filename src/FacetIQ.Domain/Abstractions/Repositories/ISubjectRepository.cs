using FacetIQ.Domain.Entities;

namespace FacetIQ.Domain.Abstractions.Repositories;

public interface ISubjectRepository
{
    Task<Subject?> FindAsync(Guid subjectId, CancellationToken cancellationToken);
}
