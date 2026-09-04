using FacetIQ.Domain.Enums;

namespace FacetIQ.Domain.Entities;

/// <summary>
/// One claim a subject holds. Several attributes commonly share a <see cref="Key"/> --
/// a subject may hold five distinct names -- and selecting between them is the system's
/// primary mechanism.
/// </summary>
public sealed class SubjectAttribute
{
    public Guid Id { get; init; }

    public Guid SubjectId { get; init; }

    /// <summary>What kind of claim this is, for example "name" or "dateOfBirth".</summary>
    public required string Key { get; init; }

    public required string Value { get; init; }

    /// <summary>
    /// The subject's own description of when this claim applies, for example "religious".
    /// Carried for the authoring interface; the engine selects by norm, not by label.
    /// </summary>
    public string? Label { get; init; }

    /// <summary>
    /// What this claim was collected for, where a purpose was stated. Null records no limit,
    /// so the norm alone decides. A stated purpose binds the engine even where a norm permits.
    /// </summary>
    public Purpose? CollectedFor { get; init; }
}
