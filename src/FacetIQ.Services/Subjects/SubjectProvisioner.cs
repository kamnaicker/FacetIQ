using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;

namespace FacetIQ.Services.Subjects;

/// <summary>Gives an account its subject, once. Safe to call repeatedly.</summary>
public sealed class SubjectProvisioner
{
    private readonly ISubjectRepository _subjects;

    public SubjectProvisioner(ISubjectRepository subjects)
    {
        _subjects = subjects;
    }

    public async Task<Subject> EnsureAsync(string userId, CancellationToken cancellationToken)
    {
        var existing = await _subjects.FindByUserIdAsync(userId, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var subject = new Subject { Id = Guid.NewGuid(), UserId = userId };

        await _subjects.AddAsync(subject, cancellationToken);

        return subject;
    }
}
