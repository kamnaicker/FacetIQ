using System.Security.Claims;
using FacetIQ.API.Controllers;
using FacetIQ.Contracts.Disclosure;
using FacetIQ.Domain.Abstractions.Services;
using FacetIQ.Domain.Models;
using Microsoft.AspNetCore.Http;
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
        new(evaluator)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = caller }
            }
        };

    private static DisclosureRequestDto Asking(string attributeKey) => new()
    {
        SubjectId = Guid.NewGuid(),
        AttributeKey = attributeKey,
        Purpose = "Social"
    };

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
