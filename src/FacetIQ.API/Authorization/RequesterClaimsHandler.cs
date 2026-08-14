using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace FacetIQ.API.Authorization;

public sealed class RequesterClaimsRequirement : IAuthorizationRequirement;

public sealed class RequesterClaimsHandler : AuthorizationHandler<RequesterClaimsRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RequesterClaimsRequirement requirement)
    {
        if (context.User.FindFirst(ClaimTypes.NameIdentifier) is not null)
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
