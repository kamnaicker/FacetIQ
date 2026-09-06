using FacetIQ.Domain.Enums;

namespace FacetIQ.Domain.Abstractions.Services;

/// <summary>
/// Shapes a selected claim into the form a norm calls for. The transform receives no
/// request context: by this point the contextual decision is already made.
/// </summary>
public interface ITransformService
{
    string Apply(TransformKind kind, string? parameter, string value);
}
