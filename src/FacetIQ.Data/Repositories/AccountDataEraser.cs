using FacetIQ.Data.Context;
using FacetIQ.Domain.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FacetIQ.Data.Repositories;

public sealed class AccountDataEraser : IAccountDataEraser
{
    private readonly FacetIQDbContext _context;

    public AccountDataEraser(FacetIQDbContext context)
    {
        _context = context;
    }

    public async Task EraseAsync(string userId, CancellationToken cancellationToken)
    {
        var subjectIds = _context.Subjects
            .Where(subject => subject.UserId == userId)
            .Select(subject => subject.Id);

        // One transaction, so a failure part way leaves the account as it was.
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        // Rules first: a rule's claim is restricted from deletion while the rule exists.
        await _context.Norms
            .Where(norm => subjectIds.Contains(norm.SubjectId))
            .ExecuteDeleteAsync(cancellationToken);

        // Standings this account issued, and standings others issued about it.
        await _context.Standings
            .Where(standing => subjectIds.Contains(standing.SubjectId) || standing.RequesterUserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        await _context.SubjectAttributes
            .Where(attribute => subjectIds.Contains(attribute.SubjectId))
            .ExecuteDeleteAsync(cancellationToken);

        await _context.Subjects
            .Where(subject => subject.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
