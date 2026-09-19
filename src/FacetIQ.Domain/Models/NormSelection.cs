using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;

namespace FacetIQ.Domain.Models;

/// <summary>Result of ranking. Norm is set only when Outcome is Selected.</summary>
public sealed record NormSelection(SelectionOutcome Outcome, Norm? Norm)
{
    public static NormSelection NoMatch { get; } = new(SelectionOutcome.NoMatch, null);

    public static NormSelection Ambiguous { get; } = new(SelectionOutcome.Ambiguous, null);

    public static NormSelection Of(Norm norm) => new(SelectionOutcome.Selected, norm);
}
