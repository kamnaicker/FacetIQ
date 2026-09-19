namespace FacetIQ.API.Email;

// Caps how often one address can be emailed, whoever triggers it. In memory, so it resets on restart.
public sealed class RecipientThrottle
{
    private const int DailyLimit = 5;

    private static readonly TimeSpan MinimumGap = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan Window = TimeSpan.FromDays(1);
    private static readonly TimeSpan PruneInterval = TimeSpan.FromHours(1);

    private readonly TimeProvider _clock;
    private readonly Dictionary<string, List<DateTimeOffset>> _sent = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _gate = new();
    private DateTimeOffset _lastPruned = DateTimeOffset.MinValue;

    public RecipientThrottle(TimeProvider clock)
    {
        _clock = clock;
    }

    /// <summary>True, and recorded as sent, when the address is within its limits.</summary>
    public bool TryAcquire(string address)
    {
        var now = _clock.GetUtcNow();

        lock (_gate)
        {
            PruneIfDue(now);

            if (!_sent.TryGetValue(address, out var sentAt))
            {
                sentAt = [];
                _sent[address] = sentAt;
            }

            sentAt.RemoveAll(time => now - time >= Window);

            if (sentAt.Count >= DailyLimit || (sentAt.Count > 0 && now - sentAt[^1] < MinimumGap))
            {
                return false;
            }

            sentAt.Add(now);

            return true;
        }
    }

    // Drops addresses with nothing inside the window, so made-up addresses cannot grow memory forever.
    private void PruneIfDue(DateTimeOffset now)
    {
        if (now - _lastPruned < PruneInterval)
        {
            return;
        }

        foreach (var address in _sent.Keys.ToList())
        {
            if (_sent[address].All(time => now - time >= Window))
            {
                _sent.Remove(address);
            }
        }

        _lastPruned = now;
    }
}
