namespace FacetIQ.Data.Identity;

public interface IPendingRegistrationStore
{
    Task AddAsync(PendingRegistration pending, CancellationToken cancellationToken);

    Task<PendingRegistration?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<PendingRegistration?> FindByCancellationTokenAsync(string tokenHash, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Increments Attempts in a single statement so concurrent guesses cannot lose a count. Returns
    /// the new value, or 0 if no row matched.
    /// </summary>
    Task<int> RecordFailedAttemptAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Deletes every attempt for the address, matched on NormalizedEmail.</summary>
    Task DeleteForEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task DeleteExpiredForEmailAsync(string normalizedEmail, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically reserves a resend: increments Resends and sets LastSentAt to now, but only while the
    /// row is unexpired, under the resend cap, and its LastSentAt has not moved past sentNoLaterThan.
    /// Two concurrent callers can therefore never both win the same cooldown window. Returns whether
    /// this call was the one that won.
    /// </summary>
    Task<bool> TryClaimResendAsync(
        Guid id,
        DateTimeOffset now,
        DateTimeOffset sentNoLaterThan,
        int maxResends,
        CancellationToken cancellationToken);

    /// <summary>Undoes a claim that turned out not to send anything, restoring the prior LastSentAt.</summary>
    Task ReleaseResendAsync(Guid id, DateTimeOffset previousLastSentAt, CancellationToken cancellationToken);

    /// <summary>Installs the code a claimed resend sent, and clears attempts against the old one.</summary>
    Task ReplaceCodeAsync(Guid id, string codeHash, DateTimeOffset expiresAt, CancellationToken cancellationToken);
}
