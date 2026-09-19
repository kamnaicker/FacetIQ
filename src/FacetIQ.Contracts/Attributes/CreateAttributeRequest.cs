using System.ComponentModel.DataAnnotations;
using FacetIQ.Contracts.Validation;

namespace FacetIQ.Contracts.Attributes;

/// <summary>The subject comes from the token. Lengths mirror SubjectAttributeConfiguration.</summary>
public sealed record CreateAttributeRequest
{
    [Required]
    [StringLength(64)]
    [RegularExpression(ClaimKey.Pattern, ErrorMessage = ClaimKey.Message)]
    public required string Key { get; init; }

    // No pattern, so hyphens, apostrophes, non-Latin scripts and single names are all valid.
    // Kinds with a fixed format are checked by IClaimValueValidator.
    [Required]
    [StringLength(512)]
    [PlainText]
    public required string Value { get; init; }

    /// <summary>The subject's own description of when this claim applies. Not used in matching.</summary>
    [StringLength(64)]
    [PlainText]
    public string? Label { get; init; }

    /// <summary>Purpose the claim was collected for. Null means no limit.</summary>
    [StringLength(32)]
    public string? CollectedFor { get; init; }
}
