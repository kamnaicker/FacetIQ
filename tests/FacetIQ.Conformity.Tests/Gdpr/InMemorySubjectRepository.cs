using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;

namespace FacetIQ.Conformity.Tests.Gdpr;

/// <summary>
/// Holds at most one subject. Constructed empty, no caller owns the subject under test, so
/// every request takes the governed path.
/// </summary>
internal sealed class InMemorySubjectRepository : ISubjectRepository
{
    private Subject? _subject;

    public InMemorySubjectRepository(Subject? subject = null) => _subject = subject;

    public Task<Subject?> FindAsync(Guid subjectId, CancellationToken cancellationToken) =>
        Task.FromResult(_subject?.Id == subjectId ? _subject : null);

    public Task<Subject?> FindByUserIdAsync(string userId, CancellationToken cancellationToken) =>
        Task.FromResult(_subject?.UserId == userId ? _subject : null);

    public Task AddAsync(Subject subject, CancellationToken cancellationToken)
    {
        _subject = subject;

        return Task.CompletedTask;
    }
}
