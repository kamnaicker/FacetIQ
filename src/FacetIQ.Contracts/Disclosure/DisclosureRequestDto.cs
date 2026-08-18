namespace FacetIQ.Contracts.Disclosure;

/// <summary>
/// What a requester sends. The requester's own identity is deliberately absent: it comes
/// from the authenticated principal, so a caller cannot describe themselves into a more
/// favourable context.
/// </summary>
public sealed record DisclosureRequestDto
{
    public required Guid SubjectId { get; init; }

    public required string AttributeKey { get; init; }

    public required string Purpose { get; init; }

    public string? Relationship { get; init; }
}
