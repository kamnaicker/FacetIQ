using FacetIQ.Domain.Abstractions.Services;
using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;
using FacetIQ.Domain.Models;

namespace FacetIQ.Services.Authoring;

public sealed class ConflictDetector : IConflictDetector
{
    public IReadOnlyList<NormConflict> Detect(Norm proposed, IReadOnlyList<Norm> existing)
    {
        var conflicts = new List<NormConflict>();
        var specificity = NormSpecificity.Of(proposed);

        foreach (var norm in existing)
        {
            if (norm.SupersededAt is null &&
                NormSpecificity.Of(norm) == specificity &&
                CoMatchable(proposed, norm) &&
                Disagree(proposed, norm))
            {
                conflicts.Add(NormConflict.Between(proposed, norm));
            }
        }

        return conflicts;
    }

    /// <summary>
    /// Whether one request could satisfy both norms. A null condition is a wildcard and intersects
    /// anything. Relationship is compared case-insensitively, as matching tests it against
    /// held standings.
    /// </summary>
    private static bool CoMatchable(Norm a, Norm b) =>
        CanIntersect(a.Relationship, b.Relationship) &&
        CanIntersect(a.Purpose, b.Purpose);

    private static bool CanIntersect(string? a, string? b) =>
        a is null || b is null ||
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static bool CanIntersect(Purpose? a, Purpose? b) =>
        a is null || b is null || a == b;

    /// <summary>
    /// Whether the two would release different things. The action alone is not enough: two norms
    /// can both return and still select different claims, or transform the same one differently.
    /// </summary>
    private static bool Disagree(Norm a, Norm b) =>
        a.AttributeId != b.AttributeId ||
        a.Action != b.Action ||
        a.Transform != b.Transform ||
        !string.Equals(a.TransformParameter, b.TransformParameter, StringComparison.Ordinal);
}
