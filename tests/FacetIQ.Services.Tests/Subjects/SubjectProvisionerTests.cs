using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;
using FacetIQ.Services.Subjects;

namespace FacetIQ.Services.Tests.Subjects;

public class SubjectProvisionerTests
{
    [Fact]
    public async Task AccountWithoutASubject_GetsOne()
    {
        var subjects = new InMemorySubjects();

        var subject = await new SubjectProvisioner(subjects).EnsureAsync("riya", CancellationToken.None);

        Assert.Equal("riya", subject.UserId);
        Assert.Same(subject, Assert.Single(subjects.Rows));
    }

    [Fact]
    public async Task AccountWithASubject_KeepsIt()
    {
        var existing = new Subject { Id = Guid.NewGuid(), UserId = "riya" };
        var subjects = new InMemorySubjects(existing);

        var subject = await new SubjectProvisioner(subjects).EnsureAsync("riya", CancellationToken.None);

        Assert.Same(existing, subject);
        Assert.Single(subjects.Rows);
    }

    /// <summary>Two confirmations can arrive at once, for example a mail scanner beside the person.</summary>
    [Fact]
    public async Task TwoRequestsAtOnce_ShareTheSubjectThatWasStored()
    {
        var winner = new Subject { Id = Guid.NewGuid(), UserId = "riya" };
        var subjects = new InMemorySubjects { StoredDuringAdd = winner };

        var subject = await new SubjectProvisioner(subjects).EnsureAsync("riya", CancellationToken.None);

        Assert.Same(winner, subject);
        Assert.Same(winner, Assert.Single(subjects.Rows));
    }

    private sealed class InMemorySubjects : ISubjectRepository
    {
        public InMemorySubjects(params Subject[] subjects)
        {
            Rows = [.. subjects];
        }

        public List<Subject> Rows { get; }

        /// <summary>Set to the row another request stored while this one was deciding.</summary>
        public Subject? StoredDuringAdd { get; init; }

        public Task<Subject?> FindAsync(Guid subjectId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Rows.SingleOrDefault(subject => subject.Id == subjectId));
        }

        public Task<Subject?> FindByUserIdAsync(string userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Rows.SingleOrDefault(subject => subject.UserId == userId));
        }

        public Task<Subject> AddOrGetAsync(Subject subject, CancellationToken cancellationToken)
        {
            if (StoredDuringAdd is not null)
            {
                Rows.Add(StoredDuringAdd);

                return Task.FromResult(StoredDuringAdd);
            }

            Rows.Add(subject);

            return Task.FromResult(subject);
        }
    }
}
