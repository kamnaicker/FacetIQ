using System.ComponentModel.DataAnnotations;

namespace FacetIQ.Contracts.Attributes;

/// <summary>
/// A claim the subject is adding. The subject is absent deliberately: it comes from the
/// authenticated principal, so a caller cannot write claims into someone else's profile.
///
/// A new claim is an addition to the set, never a correction of it. Lengths mirror the columns
/// in SubjectAttributeConfiguration.
/// </summary>
public sealed record CreateAttributeRequest
{
    [Required]
    [StringLength(64)]
    public required string Key { get; init; }

    /// <summary>
    /// Bounded in length and in nothing else.
    ///
    /// No pattern is imposed on a claim's content, and that is a decision rather than an
    /// oversight. A validator that rejects a hyphen, an apostrophe, a non-Latin script or a name
    /// of one word is the precise failure this project exists to argue against: whatever a person
    /// tells you their name is, is their name. The only thing checked here is that the value fits
    /// the column, because that is the only thing the system has any standing to insist on.
    /// </summary>
    [Required]
    [StringLength(512)]
    public required string Value { get; init; }

    /// <summary>
    /// The subject's own words for when this claim applies, never interpreted -- the engine
    /// selects by norm. It is here so a person states their context rather than the system
    /// inferring it from their data.
    /// </summary>
    [StringLength(64)]
    public string? Label { get; init; }

    /// <summary>
    /// What this claim was collected for. Null records no limit. A stated purpose binds the engine
    /// even where a norm would permit the disclosure, so it only ever narrows.
    /// </summary>
    [StringLength(32)]
    public string? CollectedFor { get; init; }
}
