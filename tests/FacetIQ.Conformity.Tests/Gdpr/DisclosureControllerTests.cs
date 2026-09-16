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
    /// <summary>O3: refused before evaluation. The call count, not the status, proves it.</summary>
    [Fact]
    public async Task UnauthenticatedRequest_NeverReachesEvaluation()
    {
        var evaluator = new UnreachableEvaluator();
        var controller = ControllerFor(evaluator, new ClaimsPrincipal(new ClaimsIdentity()));

        var response = await controller.Post(Asking("name"), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(response.Result);
        Assert.Equal(0, evaluator.Calls);
    }

    /// <summary>O3: the control for the test above.</summary>
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

    /// <summary>Resolves no email. An unknown address still reaches the engine.</summary>
    private sealed class NoAccounts()
        : UserManager<AppUser>(new EmptyUserStore(), null!, null!, null!, null!, null!, null!, null!, null!)
    {
        public override Task<AppUser?> FindByEmailAsync(string email) => Task.FromResult<AppUser?>(null);
    }

    /// <summary>Counts calls, then throws so an unexpected call cannot pass silently.</summary>
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
