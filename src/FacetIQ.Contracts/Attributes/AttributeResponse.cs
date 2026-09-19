namespace FacetIQ.Contracts.Attributes;

public sealed record AttributeResponse
{
    public required Guid Id { get; init; }

    public required string Key { get; init; }

    public required string Value { get; init; }

    public string? Label { get; init; }

    public string? CollectedFor { get; init; }
}
