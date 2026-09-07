using FacetIQ.Domain.Entities;

namespace FacetIQ.Domain.Models;

/// <summary>
/// The one definition of specificity, called by matching at request time and by conflict
/// detection at authoring time. Static rather than injectable so the two cannot disagree.
/// </summary>
public static class NormSpecificity
{
    /// <summary>
    /// Counts a norm's bound conditions. The score is a property of the norm alone and does not
    /// vary with the request, which is what lets ambiguity be caught when a norm is authored.
    /// </summary>
    public static int Of(Norm norm) =>
        (norm.Relationship is null ? 0 : 1) +
        (norm.Purpose is null ? 0 : 1);
}
