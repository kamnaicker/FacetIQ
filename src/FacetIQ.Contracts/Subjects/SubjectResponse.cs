namespace FacetIQ.Contracts.Subjects;

/// <summary>The caller's own profile.</summary>
public sealed record SubjectResponse
{
    public required Guid Id { get; init; }
}
