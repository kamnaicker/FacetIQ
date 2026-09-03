using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;

namespace FacetIQ.Services.Tests;

/// <summary>
/// Applies the same filtering rules as the EF repository against an in-memory list, so a
/// test exercises the decision logic rather than query translation.
/// </summary>
internal sealed class InMemoryNormRepository : INormRepository
{
    private readonly IReadOnlyList<Norm> _norms;

    public InMemoryNormRepository(params Norm[] norms) => _norms = norms;

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
}
