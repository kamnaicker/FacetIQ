using FacetIQ.Domain.Abstractions.Services;
using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;
using FacetIQ.Domain.Models;

namespace FacetIQ.Services.Authoring;

public sealed class ConflictDetector : IConflictDetector
{
    // Mirrors SpecificityRanker: any two norms one request can match at equal specificity tie,
    // whether or not they release the same thing.
    public IReadOnlyList<NormConflict> Detect(Norm proposed, IReadOnlyList<Norm> existing)
    {
        var specificity = NormSpecificity.Of(proposed);

        return existing
            .Where(norm =>
                norm.SupersededAt is null &&
                NormSpecificity.Of(norm) == specificity &&
                CoMatchable(proposed, norm))
            .Select(norm => NormConflict.Between(proposed, norm))
            .ToList();
    }

    // Null is a wildcard. Two different relationships are treated as disjoint even though a
    // requester can hold both; that requester is refused as ambiguous at request time instead.
    private static bool CoMatchable(Norm a, Norm b) =>
        CanIntersect(a.Relationship, b.Relationship) &&
        CanIntersect(a.Purpose, b.Purpose);

    private static bool CanIntersect(string? a, string? b) =>
        a is null || b is null ||
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static bool CanIntersect(Purpose? a, Purpose? b) =>
        a is null || b is null || a == b;
}
