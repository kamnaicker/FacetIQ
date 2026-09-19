using System.Net;
using System.Threading.RateLimiting;
using FacetIQ.API.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace FacetIQ.API.Tests.RateLimiting;

public class AccountEmailRateLimitTests
{
    [Theory]
    [InlineData("/register")]
    [InlineData("/resendConfirmationEmail")]
    [InlineData("/forgotPassword")]
    [InlineData("/Register")]
    public void EndpointsThatSendEmail_AreLimited(string path)
    {
        Assert.True(AccountEmailRateLimit.Applies(Request("POST", path, "203.0.113.5")));
    }

    [Theory]
    [InlineData("POST", "/login")]
    [InlineData("POST", "/disclosure")]
    [InlineData("GET", "/register")]
    public void OtherRequests_AreNotLimited(string method, string path)
    {
        Assert.False(AccountEmailRateLimit.Applies(Request(method, path, "203.0.113.5")));
    }

    [Fact]
    public void SixthRequestFromOneAddress_IsRejected()
    {
        using var limiter = Limiter();
        var request = Request("POST", "/resendConfirmationEmail", "203.0.113.5");

        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var lease = limiter.AttemptAcquire(request);
            Assert.True(lease.IsAcquired);
        }

        using var rejected = limiter.AttemptAcquire(request);
        Assert.False(rejected.IsAcquired);
    }

    [Fact]
    public void Limits_AreCountedAcrossTheThreeEndpoints()
    {
        using var limiter = Limiter();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var lease = limiter.AttemptAcquire(Request("POST", "/register", "203.0.113.5"));
        }

        using var rejected = limiter.AttemptAcquire(Request("POST", "/forgotPassword", "203.0.113.5"));
        Assert.False(rejected.IsAcquired);
    }

    [Fact]
    public void AnotherAddress_HasItsOwnLimit()
    {
        using var limiter = Limiter();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var lease = limiter.AttemptAcquire(Request("POST", "/register", "203.0.113.5"));
        }

        using var other = limiter.AttemptAcquire(Request("POST", "/register", "198.51.100.7"));
        Assert.True(other.IsAcquired);
    }

    [Fact]
    public void Login_IsNeverRejected()
    {
        using var limiter = Limiter();
        var request = Request("POST", "/login", "203.0.113.5");

        for (var attempt = 0; attempt < 20; attempt++)
        {
            using var lease = limiter.AttemptAcquire(request);
            Assert.True(lease.IsAcquired);
        }
    }

    private static PartitionedRateLimiter<HttpContext> Limiter()
    {
        return PartitionedRateLimiter.Create<HttpContext, string>(AccountEmailRateLimit.Partition);
    }

    private static HttpContext Request(string method, string path, string address)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Connection.RemoteIpAddress = IPAddress.Parse(address);

        return context;
    }
}
