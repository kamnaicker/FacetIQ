using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;

namespace FacetIQ.Services.Tests;

internal sealed class InMemoryAttributeRepository : IAttributeRepository
{
    private readonly IReadOnlyList<SubjectAttribute> _attributes;

    public InMemoryAttributeRepository(params SubjectAttribute[] attributes) => _attributes = attributes;

    public Task<SubjectAttribute?> FindAsync(Guid attributeId, CancellationToken cancellationToken) =>
        Task.FromResult(_attributes.SingleOrDefault(attribute => attribute.Id == attributeId));
}
