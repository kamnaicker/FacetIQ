using System.Text;
using FacetIQ.API.Identity;
using Microsoft.AspNetCore.Http;

namespace FacetIQ.API.Tests.Identity;

public class ClosedIdentityEndpointsMiddlewareTests
{
    [Theory]
    [InlineData("/register")]
    [InlineData("/register/")]
    [InlineData("/Register")]
    [InlineData("/REGISTER/")]
    [InlineData("/resendConfirmationEmail")]
    [InlineData("/resendConfirmationEmail/")]
    [InlineData("/RESENDCONFIRMATIONEMAIL")]
    public async Task ClosedPath_IsRefusedWithAnEmptyNotFound(string path)
    {
        foreach (var body in WellFormedMalformedAndEmptyBodies())
        {
            var nextCalled = false;
            var context = ContextFor("POST", path, body);

            await Middleware(() => nextCalled = true).InvokeAsync(context);

            Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
            Assert.Equal(0, context.Response.Body.Length);
            Assert.False(nextCalled, "the closed route must never reach the next delegate, which is where a body would be parsed");
        }
    }

    [Theory]
    [InlineData("POST", "/login")]
    [InlineData("GET", "/confirmEmail")]
    [InlineData("POST", "/registration")]
    [InlineData("POST", "/registration/")]
    [InlineData("POST", "/registration/confirm")]
    [InlineData("POST", "/registration/resend")]
    [InlineData("POST", "/registration/cancel")]
    public async Task UnaffectedPath_ReachesTheNextDelegate(string method, string path)
    {
        var nextCalled = false;
        var context = ContextFor(method, path, "{}");

        await Middleware(() => nextCalled = true).InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.NotEqual(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    private static IEnumerable<string> WellFormedMalformedAndEmptyBodies()
    {
        yield return "{\"email\":\"riya@example.test\",\"password\":\"A-good-password-1!\"}";
        yield return "{not valid json";
        yield return string.Empty;
    }

    private static DefaultHttpContext ContextFor(string method, string path, string body)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Response.Body = new MemoryStream();

        // Left at 200, so a middleware that sets 404 but still calls next is caught.
        return context;
    }

    private static ClosedIdentityEndpointsMiddleware Middleware(Action onNextCalled)
    {
        return new ClosedIdentityEndpointsMiddleware(context =>
        {
            onNextCalled();

            return Task.CompletedTask;
        });
    }
}
