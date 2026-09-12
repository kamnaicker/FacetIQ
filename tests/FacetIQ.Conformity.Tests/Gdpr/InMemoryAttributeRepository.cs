using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;

namespace FacetIQ.Conformity.Tests.Gdpr;

internal sealed class InMemoryAttributeRepository : IAttributeRepository
{
    private readonly List<SubjectAttribute> _attributes;

    public InMemoryAttributeRepository(params SubjectAttribute[] attributes) => _attributes = [.. attributes];

    public Task<SubjectAttribute?> FindAsync(Guid attributeId, CancellationToken cancellationToken) =>
        Task.FromResult(_attributes.SingleOrDefault(attribute => attribute.Id == attributeId));

    public Task<IReadOnlyList<SubjectAttribute>> ListByKeyAsync(
        Guid subjectId,
        string key,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SubjectAttribute>>(
            _attributes
                .Where(attribute => attribute.SubjectId == subjectId && attribute.Key == key)
                .ToList());

    public Task<IReadOnlyList<SubjectAttribute>> ListBySubjectAsync(
        Guid subjectId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SubjectAttribute>>(
            _attributes.Where(attribute => attribute.SubjectId == subjectId).ToList());

    public Task AddAsync(SubjectAttribute attribute, CancellationToken cancellationToken)
    {
        _attributes.Add(attribute);

        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(Guid attributeId, CancellationToken cancellationToken) =>
        Task.FromResult(_attributes.RemoveAll(attribute => attribute.Id == attributeId) > 0);
}
