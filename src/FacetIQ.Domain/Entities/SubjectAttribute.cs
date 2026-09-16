using FacetIQ.Domain.Enums;

namespace FacetIQ.Domain.Entities;

/// <summary>One claim. A subject can hold several under the same key, e.g. more than one name.</summary>
public sealed class SubjectAttribute
{
    public Guid Id { get; init; }

    public Guid SubjectId { get; init; }

    /// <summary>The kind of claim, e.g. "name" or "dateOfBirth".</summary>
    public required string Key { get; init; }

    public required string Value { get; init; }

    /// <summary>The subject's own description, e.g. "religious". Not used in matching.</summary>
    public string? Label { get; init; }

    /// <summary>Purpose the claim was collected for. When set, requests for any other purpose are refused.</summary>
    public Purpose? CollectedFor { get; init; }
}
