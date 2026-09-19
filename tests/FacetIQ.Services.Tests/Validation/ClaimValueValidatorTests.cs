using FacetIQ.Services.Validation;

namespace FacetIQ.Services.Tests.Validation;

public class ClaimValueValidatorTests
{
    private static readonly DateTimeOffset Today = new(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("dateOfBirth", "1994-03-11")]
    [InlineData("email", "amara@example.com")]
    [InlineData("phone", "+27821234567")]
    [InlineData("phone", "+442071838750")]
    [InlineData("pronouns", "she/her")]
    [InlineData("name", "Amara Chidinma Nwosu")]
    public void WellFormedValue_IsAccepted(string key, string value)
    {
        Assert.True(Values().IsValid(key, value, out _));
    }

    [Theory]
    [InlineData("dateOfBirth", "11/03/1994")]
    [InlineData("dateOfBirth", "2030-01-01")]
    [InlineData("dateOfBirth", "1850-01-01")]
    [InlineData("email", "banana")]
    [InlineData("email", "Amara <amara@example.com>")]
    [InlineData("phone", "0821234567")]
    [InlineData("phone", "+27 82 123 4567")]
    [InlineData("phone", "+2712")]
    [InlineData("phone", "call me")]
    public void MalformedValue_IsRefused_WithAReason(string key, string value)
    {
        Assert.False(Values().IsValid(key, value, out var problem));
        Assert.NotEmpty(problem);
    }

    private static ClaimValueValidator Values()
    {
        return new ClaimValueValidator(new FixedClock(Today));
    }

    private sealed class FixedClock : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedClock(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _now;
        }
    }
}
