using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;

namespace FacetIQ.Services.Tests;

/// <summary>
/// Holds at most one subject. Constructed empty, no caller owns the subject under test, so
/// every request takes the governed path.
/// </summary>
internal sealed class InMemorySubjectRepository : ISubjectRepository
{
    private readonly Subject? _subject;

    public InMemorySubjectRepository(Subject? subject = null) => _subject = subject;

    public Task<Subject?> FindAsync(Guid subjectId, CancellationToken cancellationToken) =>
        Task.FromResult(_subject?.Id == subjectId ? _subject : null);
}
