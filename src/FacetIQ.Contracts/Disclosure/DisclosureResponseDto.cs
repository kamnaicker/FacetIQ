namespace FacetIQ.Contracts.Disclosure;

public sealed record DisclosureResponseDto
{
    /// <summary>Return, Transform or Deny.</summary>
    public required string Outcome { get; init; }

    public string? Value { get; init; }

    /// <summary>Self-access only, with Value null: every claim under the key.</summary>
    public IReadOnlyList<string>? Values { get; init; }

    public string? DenyReason { get; init; }

    public string? JustifyingPrinciple { get; init; }
}
