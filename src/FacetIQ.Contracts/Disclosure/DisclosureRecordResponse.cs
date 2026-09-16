namespace FacetIQ.Contracts.Disclosure;

/// <summary>One audit record about the caller. Has no value field, as the record stores none.</summary>
public sealed record DisclosureRecordResponse
{
    public required Guid Id { get; init; }

    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>The asker's address, or null when the account no longer exists.</summary>
    public string? Requester { get; init; }

    /// <summary>True when the subject was reading their own claims.</summary>
    public required bool IsSelf { get; init; }

    public required string AttributeKey { get; init; }

    public required string Purpose { get; init; }

    public required string Outcome { get; init; }

    public string? DenyReason { get; init; }

    public string? Transform { get; init; }

    public string? TransformParameter { get; init; }

    public string? JustifyingPrinciple { get; init; }
}
