using System.ComponentModel.DataAnnotations;

namespace FacetIQ.Contracts.Disclosure;

/// <summary>
/// What a requester sends. The requester's own identity is deliberately absent: it comes
/// from the authenticated principal, so a caller cannot describe themselves into a more
/// favourable context.
///
/// Lengths mirror the columns these values are compared against, so oversized input is
/// refused at the edge rather than surviving as far as the database. The constraints bound
/// the shape of a request, never the content of a person's claim.
/// </summary>
public sealed record DisclosureRequestDto
{
    public required Guid SubjectId { get; init; }

    [Required]
    [StringLength(64)]
    public required string AttributeKey { get; init; }

    [Required]
    [StringLength(32)]
    public required string Purpose { get; init; }
}
