namespace FacetIQ.Contracts.Subjects;

/// <summary>The caller's own profile. Its identifier is what a requester addresses a request to.</summary>
public sealed record SubjectResponse
{
    public required Guid Id { get; init; }
}
