using FacetIQ.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace FacetIQ.Data.Identity;

public sealed class PendingRegistrationStore : IPendingRegistrationStore
{
    private readonly AuthDbContext _context;

    public PendingRegistrationStore(AuthDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(PendingRegistration pending, CancellationToken cancellationToken)
    {
        _context.PendingRegistrations.Add(pending);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PendingRegistration?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.PendingRegistrations
            .FirstOrDefaultAsync(pending => pending.Id == id, cancellationToken);
    }

    public async Task<PendingRegistration?> FindByCancellationTokenAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return await _context.PendingRegistrations
            .FirstOrDefaultAsync(pending => pending.CancellationTokenHash == tokenHash, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var pending = await _context.PendingRegistrations
            .FirstOrDefaultAsync(entry => entry.Id == id, cancellationToken);

        if (pending is not null)
        {
            _context.PendingRegistrations.Remove(pending);

            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> RecordFailedAttemptAsync(Guid id, CancellationToken cancellationToken)
    {
        var affected = await _context.PendingRegistrations
            .Where(pending => pending.Id == id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(pending => pending.Attempts, pending => pending.Attempts + 1),
                cancellationToken);

        if (affected == 0)
        {
            return 0;
        }

        // ExecuteUpdateAsync bypasses change tracking, so read the value back untracked.
        return await _context.PendingRegistrations
            .AsNoTracking()
            .Where(pending => pending.Id == id)
            .Select(pending => pending.Attempts)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task DeleteForEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        var matches = await _context.PendingRegistrations
            .Where(pending => pending.NormalizedEmail == normalizedEmail)
            .ToListAsync(cancellationToken);

        _context.PendingRegistrations.RemoveRange(matches);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteExpiredForEmailAsync(string normalizedEmail, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var expired = await _context.PendingRegistrations
            .Where(pending => pending.NormalizedEmail == normalizedEmail && pending.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        _context.PendingRegistrations.RemoveRange(expired);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TryClaimResendAsync(
        Guid id,
        DateTimeOffset now,
        DateTimeOffset sentNoLaterThan,
        int maxResends,
        CancellationToken cancellationToken)
    {
        var affected = await _context.PendingRegistrations
            .Where(pending => pending.Id == id
                && pending.ExpiresAt > now
                && pending.Resends < maxResends
                && pending.LastSentAt <= sentNoLaterThan)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(pending => pending.Resends, pending => pending.Resends + 1)
                    .SetProperty(pending => pending.LastSentAt, now),
                cancellationToken);

        return affected == 1;
    }

    public async Task ReleaseResendAsync(Guid id, DateTimeOffset previousLastSentAt, CancellationToken cancellationToken)
    {
        await _context.PendingRegistrations
            .Where(pending => pending.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(pending => pending.Resends, pending => pending.Resends - 1)
                    .SetProperty(pending => pending.LastSentAt, previousLastSentAt),
                cancellationToken);
    }

    public async Task ReplaceCodeAsync(Guid id, string codeHash, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        await _context.PendingRegistrations
            .Where(pending => pending.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(pending => pending.CodeHash, codeHash)
                    .SetProperty(pending => pending.Attempts, 0)
                    .SetProperty(pending => pending.ExpiresAt, expiresAt),
                cancellationToken);
    }
}
