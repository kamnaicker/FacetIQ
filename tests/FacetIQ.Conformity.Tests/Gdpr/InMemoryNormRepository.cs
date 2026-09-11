using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;

namespace FacetIQ.Conformity.Tests.Gdpr;

/// <summary>
/// Applies the same filtering rules as the EF repository against an in-memory list, so a
/// test exercises the decision logic rather than query translation.
/// </summary>
internal sealed class InMemoryNormRepository : INormRepository
{
    private readonly List<Norm> _norms;

    public InMemoryNormRepository(params Norm[] norms) => _norms = [.. norms];

    public Task<IReadOnlyList<Norm>> GetGoverningNormsAsync(
        Guid subjectId,
        string attributeKey,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Norm> governing = _norms
            .Where(norm =>
                norm.SubjectId == subjectId &&
                norm.SupersededAt is null &&
                norm.Attribute.Key == attributeKey)
            .ToList();

        return Task.FromResult(governing);
    }

    public Task<IReadOnlyList<Norm>> ListGoverningAsync(
        Guid subjectId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Norm> governing = _norms
            .Where(norm =>
                norm.SubjectId == subjectId &&
                norm.SupersededAt is null)
            .ToList();

        return Task.FromResult(governing);
    }

    public Task AddAsync(Norm norm, CancellationToken cancellationToken)
    {
        _norms.Add(norm);

        return Task.CompletedTask;
    }
}
