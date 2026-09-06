using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;

namespace FacetIQ.Domain.Models;

/// <summary>
/// The result of ranking candidates. Ambiguity is a distinct outcome rather than an error,
/// because an unresolved tie is a legitimate state the engine must refuse to guess past.
/// </summary>
public sealed record NormSelection(SelectionOutcome Outcome, Norm? Norm)
{
    public static NormSelection NoMatch { get; } = new(SelectionOutcome.NoMatch, null);

    public static NormSelection Ambiguous { get; } = new(SelectionOutcome.Ambiguous, null);

    public static NormSelection Of(Norm norm) => new(SelectionOutcome.Selected, norm);
}
