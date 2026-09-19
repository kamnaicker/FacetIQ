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

    private sealed class InMemorySubjects : ISubjectRepository
    {
        public InMemorySubjects(params Subject[] subjects)
        {
            Rows = [.. subjects];
        }

        public List<Subject> Rows { get; }

        public Task<Subject?> FindAsync(Guid subjectId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Rows.SingleOrDefault(subject => subject.Id == subjectId));
        }

        public Task<Subject?> FindByUserIdAsync(string userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Rows.SingleOrDefault(subject => subject.UserId == userId));
        }

        public Task AddAsync(Subject subject, CancellationToken cancellationToken)
        {
            Rows.Add(subject);

            return Task.CompletedTask;
        }
    }
}
