namespace FacetIQ.Domain.Enums;

/// <summary>How a selected claim is shaped before release. See TransformService.</summary>
public enum TransformKind
{
    None,
    Redact,
    Reformat,
    Generalise
}
