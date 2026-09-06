using FacetIQ.Domain.Entities;

namespace FacetIQ.Domain.Models;

/// <summary>
/// The one definition of how specific a norm is, shared by the two places that need it.
///
/// Matching scores a norm when a request arrives; conflict detection scores a pair of norms when
/// one is authored. Those two must agree, or a norm could be accepted as unambiguous and then tie
/// at request time. A single function they both call is what makes them agree by construction,
/// rather than by two implementations being kept in step.
///
/// It is deliberately not an injectable service. There is nothing here to configure or substitute,
/// and making the score swappable would reintroduce the disagreement this exists to prevent.
/// </summary>
public static class NormSpecificity
{
    /// <summary>
    /// Counts a norm's bound conditions. Because a bound condition must match exactly for the norm
    /// to apply at all, the score does not vary with the request: it is a property of the norm
    /// alone, which is what allows ambiguity to be detected at authoring time rather than only
    /// when a request arrives.
    /// </summary>
    public static int Of(Norm norm) =>
        (norm.Relationship is null ? 0 : 1) +
        (norm.Purpose is null ? 0 : 1);
}
