using System.ComponentModel.DataAnnotations;

namespace FacetIQ.Contracts.Standings;

/// <summary>The issuer is the caller. The standing has no effect until the holder accepts it.</summary>
public sealed record IssueStandingRequest
{
    /// <summary>The holder's account email.</summary>
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public required string Email { get; init; }

    /// <summary>The relationship term norms match against, e.g. "colleague".</summary>
    [Required]
    [StringLength(64)]
    public required string Value { get; init; }
}
