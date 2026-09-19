using System.Threading.RateLimiting;

namespace FacetIQ.API.RateLimiting;

// Limits, per client IP, the Identity endpoints that send email. Behind a proxy the IP needs forwarded headers.
public static class AccountEmailRateLimit
{
    private const int PermitLimit = 5;
    private const string Unlimited = "unlimited";

    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
    private static readonly string[] Paths = ["/register", "/resendConfirmationEmail", "/forgotPassword"];

    public static bool Applies(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method))
        {
            return false;
        }

        foreach (var path in Paths)
        {
            if (context.Request.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static RateLimitPartition<string> Partition(HttpContext context)
    {
        if (!Applies(context))
        {
            return RateLimitPartition.GetNoLimiter(Unlimited);
        }

        var client = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(client, WindowFor);
    }

    private static FixedWindowRateLimiterOptions WindowFor(string client)
    {
        return new FixedWindowRateLimiterOptions
        {
            PermitLimit = PermitLimit,
            Window = Window,
            QueueLimit = 0
        };
    }
}
