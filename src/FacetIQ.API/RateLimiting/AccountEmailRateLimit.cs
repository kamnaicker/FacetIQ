using System.Threading.RateLimiting;
using FacetIQ.API.Routing;

namespace FacetIQ.API.RateLimiting;

// Limits, per client IP, the Identity endpoints that send email. Behind a proxy the IP needs forwarded headers.
public static class AccountEmailRateLimit
{
    private const int PermitLimit = 5;
    private const string Unlimited = "unlimited";

    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
    // Resends are already capped per attempt and keyed by an unguessable id, so the per-IP limit
    // would only add friction there.
    private static readonly string[] Paths = ["/registration", "/forgotPassword"];

    public static bool Applies(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method))
        {
            return false;
        }

        // A trailing slash reaches the same handler, so it must be counted too.
        var path = ClosedPathMatch.Normalize(context.Request.Path.Value ?? string.Empty);

        foreach (var candidate in Paths)
        {
            if (string.Equals(path, candidate, StringComparison.OrdinalIgnoreCase))
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
