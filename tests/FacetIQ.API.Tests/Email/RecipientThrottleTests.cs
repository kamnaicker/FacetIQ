using FacetIQ.API.Email;

namespace FacetIQ.API.Tests.Email;

public class RecipientThrottleTests
{
    private const string Riya = "riya@example.test";

    [Fact]
    public void FirstEmail_IsAllowed()
    {
        var throttle = new RecipientThrottle(new AdjustableClock());

        Assert.True(throttle.TryAcquire(Riya));
    }

    [Fact]
    public void SecondEmailWithinAMinute_IsRefused()
    {
        var clock = new AdjustableClock();
        var throttle = new RecipientThrottle(clock);

        throttle.TryAcquire(Riya);
        clock.Advance(TimeSpan.FromSeconds(59));

        Assert.False(throttle.TryAcquire(Riya));
    }

    [Fact]
    public void EmailAfterAMinute_IsAllowed()
    {
        var clock = new AdjustableClock();
        var throttle = new RecipientThrottle(clock);

        throttle.TryAcquire(Riya);
        clock.Advance(TimeSpan.FromMinutes(1));

        Assert.True(throttle.TryAcquire(Riya));
    }

    [Fact]
    public void SixthEmailInADay_IsRefused_UntilTheFirstIsADayOld()
    {
        var clock = new AdjustableClock();
        var throttle = new RecipientThrottle(clock);

        for (var sent = 0; sent < 5; sent++)
        {
            Assert.True(throttle.TryAcquire(Riya));
            clock.Advance(TimeSpan.FromHours(1));
        }

        Assert.False(throttle.TryAcquire(Riya));

        clock.Advance(TimeSpan.FromHours(19));

        Assert.True(throttle.TryAcquire(Riya));
    }

    [Fact]
    public void Addresses_AreLimitedSeparately()
    {
        var throttle = new RecipientThrottle(new AdjustableClock());

        throttle.TryAcquire(Riya);

        Assert.True(throttle.TryAcquire("sam@example.test"));
    }

    [Fact]
    public void Address_IgnoresCase()
    {
        var throttle = new RecipientThrottle(new AdjustableClock());

        throttle.TryAcquire(Riya);

        Assert.False(throttle.TryAcquire("RIYA@example.test"));
    }

    private sealed class AdjustableClock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 9, 17, 9, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow()
        {
            return _now;
        }

        public void Advance(TimeSpan by)
        {
            _now += by;
        }
    }
}
