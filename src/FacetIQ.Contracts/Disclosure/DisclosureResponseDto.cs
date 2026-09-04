namespace FacetIQ.Contracts.Disclosure;

/// <summary>
/// What a requester receives. The outcome names which of Return, Transform or Deny occurred,
/// so the caller can tell a shaped value from an unmodified one without inspecting it.
/// </summary>
public sealed record DisclosureResponseDto
{
    public required string Outcome { get; init; }

    public string? Value { get; init; }

    /// <summary>
    /// Populated only when a subject reads their own claims, where every value is returned
    /// rather than one being selected. <see cref="Value"/> is null in that case.
    /// </summary>
    public IReadOnlyList<string>? Values { get; init; }

    public string? DenyReason { get; init; }

    public string? JustifyingPrinciple { get; init; }
}
