namespace FacetIQ.Domain.Abstractions.Services;

public interface IClaimValueValidator
{
    /// <summary>False when the value is malformed for its kind. Kinds without a fixed format are free text.</summary>
    bool IsValid(string key, string value, out string problem);
}
