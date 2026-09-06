namespace FacetIQ.Domain.Enums;

/// <summary>
/// How a selected claim is shaped before release. A transform produces a coarser value
/// that remains truthful of the subject; it never substitutes an unrelated one.
/// </summary>
public enum TransformKind
{
    None,
    Redact,
    Reformat,
    Generalise
}
