using System.ComponentModel.DataAnnotations;

namespace FacetIQ.Contracts.Account;

/// <summary>Deleting an account needs its password, not just a signed in session.</summary>
public sealed record DeleteAccountRequest
{
    [Required]
    [StringLength(128)]
    public required string Password { get; init; }
}
