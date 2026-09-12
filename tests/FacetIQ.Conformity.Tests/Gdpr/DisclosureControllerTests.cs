using System.Security.Claims;
using FacetIQ.API.Controllers;
using FacetIQ.Contracts.Disclosure;
using FacetIQ.Data.Identity;
using FacetIQ.Domain.Abstractions.Services;
using FacetIQ.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FacetIQ.Conformity.Tests.Gdpr;

public class DisclosureControllerTests
{
    /// <summary>
    /// O3: an unauthenticated request is refused before evaluation begins. The status alone
    /// would not show that -- a caller cannot tell a refusal from a decision to refuse -- so
    /// the assertion that matters is the one counting calls into the engine.
    /// </summary>
    [Fact]
    public async Task UnauthenticatedRequest_NeverReachesEvaluation()
    {
        var evaluator = new UnreachableEvaluator();
        var controller = ControllerFor(evaluator, new ClaimsPrincipal(new ClaimsIdentity()));

        var response = await controller.Post(Asking("name"), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(response.Result);
        Assert.Equal(0, evaluator.Calls);
    }

    /// <summary>
    /// The control for the test above: the same caller carrying an identifier does reach the
    /// engine. Without this, a controller that never evaluated at all would pass O3.
    /// </summary>
    [Fact]
    public async Task AuthenticatedRequest_ReachesTheEngine()
    {
        var evaluator = new UnreachableEvaluator();
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "requester-1")], "test");
        var controller = ControllerFor(evaluator, new ClaimsPrincipal(identity));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => controller.Post(Asking("name"), CancellationToken.None));

        Assert.Equal(1, evaluator.Calls);
    }

    private static DisclosureController ControllerFor(IDisclosureEvaluator evaluator, ClaimsPrincipal caller) =>
        new(evaluator, new InMemorySubjectRepository(), new NoAccounts())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = caller }
            }
        };

    private static DisclosureRequestDto Asking(string attributeKey) => new()
    {
        SubjectEmail = "someone@example.test",
        AttributeKey = attributeKey,
        Purpose = "Social"
    };

    /// <summary>
    /// No address resolves, which is fine here: an unknown address still reaches the engine, so
    /// the authenticated control passes and the unauthenticated test is untouched by lookup.
    /// </summary>
    private sealed class NoAccounts()
        : UserManager<AppUser>(new UnusedStore(), null!, null!, null!, null!, null!, null!, null!, null!)
    {
        public override Task<AppUser?> FindByEmailAsync(string email) => Task.FromResult<AppUser?>(null);
    }

    private sealed class UnusedStore : IUserStore<AppUser>
    {
        public void Dispose()
        {
        }

        public Task<string> GetUserIdAsync(AppUser user, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<string?> GetUserNameAsync(AppUser user, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task SetUserNameAsync(AppUser user, string? userName, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<string?> GetNormalizedUserNameAsync(AppUser user, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task SetNormalizedUserNameAsync(
            AppUser user,
            string? normalizedName,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IdentityResult> CreateAsync(AppUser user, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IdentityResult> UpdateAsync(AppUser user, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IdentityResult> DeleteAsync(AppUser user, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<AppUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<AppUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    /// <summary>
    /// Throws rather than returning, so an unnoticed call fails the test loudly. The counter
    /// is what the assertion reads, and it stays at zero if the guard holds.
    /// </summary>
    private sealed class UnreachableEvaluator : IDisclosureEvaluator
    {
        public int Calls { get; private set; }

        public Task<DisclosureResult> EvaluateAsync(
            DisclosureRequest request,
            CancellationToken cancellationToken)
        {
            Calls++;

            throw new InvalidOperationException("Evaluation was reached without an authenticated caller.");
        }
    }
}
