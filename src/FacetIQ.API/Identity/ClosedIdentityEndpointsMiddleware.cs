using FacetIQ.API.Routing;

namespace FacetIQ.API.Identity;

/// <summary>
/// Answers MapIdentityApi's /register and /resendConfirmationEmail as not found, since registration
/// goes through the code flow instead. Middleware rather than an endpoint filter, so a request body
/// is never bound: a filter would let a malformed body return 400 and reveal the route exists.
/// </summary>
public sealed class ClosedIdentityEndpointsMiddleware
{
    private static readonly string[] Closed = ["/register", "/resendConfirmationEmail"];

    private readonly RequestDelegate _next;

    public ClosedIdentityEndpointsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        // Compared the way the router matches, or POST /register/ would reach Identity's handler.
        var path = ClosedPathMatch.Normalize(context.Request.Path.Value ?? string.Empty);

        foreach (var closed in Closed)
        {
            if (string.Equals(path, closed, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;

                return Task.CompletedTask;
            }
        }

        return _next(context);
    }
}
