using FacetIQ.Domain.Enums;

namespace FacetIQ.Domain.Abstractions.Services;

public interface ITransformService
{
    /// <summary>False when the value or parameter does not suit the transform.</summary>
    bool TryApply(TransformKind kind, string? parameter, string value, out string result);
}
