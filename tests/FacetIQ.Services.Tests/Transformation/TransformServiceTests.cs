using FacetIQ.Domain.Enums;
using FacetIQ.Services.Transformation;

namespace FacetIQ.Services.Tests.Transformation;

public class TransformServiceTests
{
    private static readonly DateTimeOffset Today = new(2026, 3, 11, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void None_ReturnsValueUnchanged()
    {
        Assert.True(Transforms().TryApply(TransformKind.None, null, "Amara Nwosu", out var result));
        Assert.Equal("Amara Nwosu", result);
    }

    [Fact]
    public void Redact_HidesValue()
    {
        Assert.True(Transforms().TryApply(TransformKind.Redact, null, "Amara Nwosu", out var result));
        Assert.Equal("[redacted]", result);
    }

    [Fact]
    public void Reformat_ReturnsInitials()
    {
        Assert.True(Transforms().TryApply(TransformKind.Reformat, null, "amara  chidinma nwosu", out var result));
        Assert.Equal("A. C. N.", result);
    }

    [Theory]
    [InlineData("2008-03-11", "over 18")]
    [InlineData("2008-03-12", "under 18")]
    public void Generalise_CountsBirthdayOnTheDay(string dateOfBirth, string expected)
    {
        Assert.True(Transforms().TryApply(TransformKind.Generalise, "18", dateOfBirth, out var result));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Generalise_DefaultsThresholdTo18()
    {
        Assert.True(Transforms().TryApply(TransformKind.Generalise, null, "1994-03-11", out var result));
        Assert.Equal("over 18", result);
    }

    [Theory]
    [InlineData("Amara")]
    [InlineData("03/11/1994")]
    public void Generalise_RejectsValueThatIsNotAnIsoDate(string value)
    {
        Assert.False(Transforms().TryApply(TransformKind.Generalise, "18", value, out _));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("-5")]
    [InlineData("0")]
    public void Generalise_RejectsThresholdThatIsNotAPositiveWholeNumber(string parameter)
    {
        Assert.False(Transforms().TryApply(TransformKind.Generalise, parameter, "1994-03-11", out _));
    }

    private static TransformService Transforms() => new(new FixedClock(Today));

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
