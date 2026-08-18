using FacetIQ.Domain.Entities;

namespace FacetIQ.Domain.Abstractions.Repositories;

public interface IAttributeRepository
{
    Task<SubjectAttribute?> FindAsync(Guid attributeId, CancellationToken cancellationToken);
}
