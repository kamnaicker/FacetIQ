namespace FacetIQ.Domain.Entities;

/// <summary>A person whose claims are disclosed. Has no name field; names are claims.</summary>
public sealed class Subject
{
    public Guid Id { get; init; }

    /// <summary>Identity user id, held as a plain value so the domain does not reference Identity.</summary>
    public required string UserId { get; init; }

    public ICollection<SubjectAttribute> Attributes { get; init; } = [];
}
