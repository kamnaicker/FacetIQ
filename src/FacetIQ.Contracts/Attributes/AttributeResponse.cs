namespace FacetIQ.Contracts.Attributes;

/// <summary>
/// One claim, as its subject sees it. Several entries commonly share a key: that is the set the
/// engine selects from, and seeing them together is what makes the selection legible to the
/// person who wrote them.
/// </summary>
public sealed record AttributeResponse
{
    public required Guid Id { get; init; }

    public required string Key { get; init; }

    public required string Value { get; init; }

    public string? Label { get; init; }

    public string? CollectedFor { get; init; }
}
