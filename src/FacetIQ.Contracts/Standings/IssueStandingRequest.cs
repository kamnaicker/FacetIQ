using System.ComponentModel.DataAnnotations;

namespace FacetIQ.Contracts.Standings;

/// <summary>
/// One person telling the system what someone else is to them. The issuer is the authenticated
/// caller, and the standing does nothing until the other person accepts it.
/// </summary>
public sealed record IssueStandingRequest
{
    /// <summary>
    /// Who it is about. An address rather than an identifier, because the issuer knows the person
    /// and not their place in a database. No endpoint lists accounts.
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public required string Email { get; init; }

    /// <summary>The term a rule is matched against, for example "colleague".</summary>
    [Required]
    [StringLength(64)]
    public required string Value { get; init; }
}
