namespace FacetIQ.Domain.Entities;

/// <summary>
/// A person whose claims the system discloses. A subject deliberately has no canonical
/// name of its own: every name is a claim held in <see cref="Attributes"/>, and none of
/// them is privileged over the others.
/// </summary>
public sealed class Subject
{
    public Guid Id { get; init; }

    /// <summary>
    /// The authentication identity this subject belongs to. Held as a plain value so the
    /// domain never references the identity store.
    /// </summary>
    public required string UserId { get; init; }

    public ICollection<SubjectAttribute> Attributes { get; init; } = [];
}
