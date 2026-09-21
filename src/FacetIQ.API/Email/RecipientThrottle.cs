namespace FacetIQ.API.Email;

// Separate allowances, so registration attempts against an address cannot also starve it of the
// mail it needs to recover an account it already has.
public enum ThrottleBucket
{
    Account,
    Registration
}

// Caps how often one address can be emailed, whoever triggers it. In memory, so it resets on restart.
public sealed class RecipientThrottle
{
    private const int AccountDailyLimit = 5;
    private const int RegistrationDailyLimit = 10;

    private static readonly TimeSpan AccountMinimumGap = TimeSpan.FromMinutes(1);

    // Matches the resend cooldown, so a resend the caller is allowed to make is never silently
    // dropped here instead.
    private static readonly TimeSpan RegistrationMinimumGap = TimeSpan.FromSeconds(30);

    private static readonly TimeSpan Window = TimeSpan.FromDays(1);
    private static readonly TimeSpan PruneInterval = TimeSpan.FromHours(1);

    private readonly TimeProvider _clock;
    private readonly Dictionary<(ThrottleBucket Bucket, string Address), List<DateTimeOffset>> _sent =
        new(new BucketedAddressComparer());
    private readonly Lock _gate = new();
    private DateTimeOffset _lastPruned = DateTimeOffset.MinValue;

    public RecipientThrottle(TimeProvider clock)
    {
        _clock = clock;
    }

    /// <summary>True, and recorded as sent, when the address is within its limits for that bucket.</summary>
    public bool TryAcquire(string address, ThrottleBucket bucket)
    {
        var now = _clock.GetUtcNow();
        var key = (bucket, address);
        var minimumGap = bucket == ThrottleBucket.Registration ? RegistrationMinimumGap : AccountMinimumGap;
        var dailyLimit = bucket == ThrottleBucket.Registration ? RegistrationDailyLimit : AccountDailyLimit;

        lock (_gate)
        {
            PruneIfDue(now);

            if (!_sent.TryGetValue(key, out var sentAt))
            {
                sentAt = [];
                _sent[key] = sentAt;
            }

            sentAt.RemoveAll(time => now - time >= Window);

            if (sentAt.Count >= dailyLimit || (sentAt.Count > 0 && now - sentAt[^1] < minimumGap))
            {
                return false;
            }

            sentAt.Add(now);

            return true;
        }
    }

    // Drops entries with nothing inside the window, so made-up addresses cannot grow memory forever.
    private void PruneIfDue(DateTimeOffset now)
    {
        if (now - _lastPruned < PruneInterval)
        {
            return;
        }

        foreach (var key in _sent.Keys.ToList())
        {
            if (_sent[key].All(time => now - time >= Window))
            {
                _sent.Remove(key);
            }
        }

        _lastPruned = now;
    }

    // Addresses stay case-insensitive; the bucket is an exact enum match.
    private sealed class BucketedAddressComparer : IEqualityComparer<(ThrottleBucket Bucket, string Address)>
    {
        public bool Equals((ThrottleBucket Bucket, string Address) x, (ThrottleBucket Bucket, string Address) y)
        {
            return x.Bucket == y.Bucket && string.Equals(x.Address, y.Address, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode((ThrottleBucket Bucket, string Address) obj)
        {
            return HashCode.Combine(obj.Bucket, obj.Address.ToUpperInvariant());
        }
    }
}
