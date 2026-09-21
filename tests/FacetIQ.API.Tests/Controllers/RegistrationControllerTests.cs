using System.Linq;
using FacetIQ.API.Controllers;
using FacetIQ.API.Tests.Registration;
using FacetIQ.Contracts.Registration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FacetIQ.API.Tests.Controllers;

public class RegistrationControllerTests
{
    [Fact]
    public async Task Start_ReturnsTheRegistrationId()
    {
        var world = new RegistrationWorld();
        var controller = new RegistrationController(world.Service);

        var result = await controller.Post(
            new StartRegistrationRequest { Email = "riya@example.test", Password = "A-good-password-1!" },
            default);

        var response = Assert.IsType<StartRegistrationResponse>(
            Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(Assert.Single(world.Store.Rows).Id, response.RegistrationId);
    }

    [Fact]
    public async Task Start_AnsweringForAnAddressThatHasAnAccount_IsAGenericConflict()
    {
        var world = new RegistrationWorld();
        await world.CreateAccount("riya@example.test");
        var controller = new RegistrationController(world.Service);

        var result = await controller.Post(
            new StartRegistrationRequest { Email = "riya@example.test", Password = "A-good-password-1!" },
            default);

        // ConflictResult has no body, so nothing about the address is returned.
        Assert.IsType<ConflictResult>(result.Result);
    }

    [Fact]
    public async Task Start_WeakPassword_IsAValidationProblemKeyedByIdentityErrorCodes()
    {
        var world = new RegistrationWorld();
        var controller = new RegistrationController(world.Service);

        var result = await controller.Post(
            new StartRegistrationRequest { Email = "riya@example.test", Password = "x" },
            default);

        var refusal = Assert.IsType<ObjectResult>(result.Result, exactMatch: false);
        var problem = Assert.IsType<ValidationProblemDetails>(refusal.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Contains(problem.Errors.Keys, key => key.Contains("Password"));
        Assert.Empty(world.Store.Rows);
    }

    [Fact]
    public async Task Confirm_ReturnsNoContentAndNothingSessionShaped()
    {
        var world = new RegistrationWorld();
        var controller = new RegistrationController(world.Service);
        var started = await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);
        var code = world.CodeFor(started.RegistrationId);

        var result = await controller.Confirm(
            new ConfirmRegistrationRequest
            {
                RegistrationId = started.RegistrationId,
                Code = code,
                Password = "A-good-password-1!"
            },
            default);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Confirm_WithAWrongCode_IsAGenericValidationProblem()
    {
        var world = new RegistrationWorld();
        var controller = new RegistrationController(world.Service);
        var started = await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);

        // Derived from the real code, so it can never collide with it.
        var correctCode = int.Parse(world.CodeFor(started.RegistrationId));
        var wrongCode = ((correctCode + 1) % 1_000_000).ToString("D6");

        var result = await controller.Confirm(
            new ConfirmRegistrationRequest
            {
                RegistrationId = started.RegistrationId,
                Code = wrongCode,
                Password = "A-good-password-1!"
            },
            default);

        var refusal = Assert.IsType<ObjectResult>(result, exactMatch: false);
        var problem = Assert.IsType<ValidationProblemDetails>(refusal.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.True(problem.Errors.ContainsKey(nameof(ConfirmRegistrationRequest.Code)));
    }

    [Fact]
    public async Task Confirm_WithAWrongPassword_IsAGenericValidationProblem()
    {
        var world = new RegistrationWorld();
        var controller = new RegistrationController(world.Service);
        var started = await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);
        var code = world.CodeFor(started.RegistrationId);

        var result = await controller.Confirm(
            new ConfirmRegistrationRequest
            {
                RegistrationId = started.RegistrationId,
                Code = code,
                Password = "A-different-password-1!"
            },
            default);

        var refusal = Assert.IsType<ObjectResult>(result, exactMatch: false);
        var problem = Assert.IsType<ValidationProblemDetails>(refusal.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.True(problem.Errors.ContainsKey(nameof(ConfirmRegistrationRequest.Code)));
    }

    [Fact]
    public async Task WrongPassword_LooksExactlyLikeAWrongCode()
    {
        var world = new RegistrationWorld();
        var started = await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);
        var code = world.CodeFor(started.RegistrationId);

        var correctCode = int.Parse(code);
        var wrongCode = ((correctCode + 1) % 1_000_000).ToString("D6");

        var wrongCodeResult = await new RegistrationController(world.Service).Confirm(
            new ConfirmRegistrationRequest
            {
                RegistrationId = started.RegistrationId,
                Code = wrongCode,
                Password = "A-good-password-1!"
            },
            default);
        var wrongPasswordResult = await new RegistrationController(world.Service).Confirm(
            new ConfirmRegistrationRequest
            {
                RegistrationId = started.RegistrationId,
                Code = code,
                Password = "A-different-password-1!"
            },
            default);

        var wrongCodeRefusal = Assert.IsType<ObjectResult>(wrongCodeResult, exactMatch: false);
        var wrongPasswordRefusal = Assert.IsType<ObjectResult>(wrongPasswordResult, exactMatch: false);
        Assert.Equal(wrongCodeRefusal.StatusCode, wrongPasswordRefusal.StatusCode);

        var wrongCodeProblem = Assert.IsType<ValidationProblemDetails>(wrongCodeRefusal.Value);
        var wrongPasswordProblem = Assert.IsType<ValidationProblemDetails>(wrongPasswordRefusal.Value);
        Assert.Equal(wrongCodeProblem.Status, wrongPasswordProblem.Status);
        Assert.Equal(wrongCodeProblem.Title, wrongPasswordProblem.Title);
        Assert.Equal(wrongCodeProblem.Type, wrongPasswordProblem.Type);
        Assert.Equal(
            wrongCodeProblem.Errors.OrderBy(error => error.Key).Select(error => (error.Key, string.Join("|", error.Value))),
            wrongPasswordProblem.Errors.OrderBy(error => error.Key).Select(error => (error.Key, string.Join("|", error.Value))));
    }

    // Compares the two responses to each other rather than to a literal, so a change to the message
    // text does not weaken the check.
    [Fact]
    public async Task UnknownRegistration_LooksExactlyLikeAWrongCode()
    {
        var world = new RegistrationWorld();
        var started = await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);

        var correctCode = int.Parse(world.CodeFor(started.RegistrationId));
        var wrongCode = ((correctCode + 1) % 1_000_000).ToString("D6");

        // A fresh controller per call, since ModelState belongs to the instance.
        var wrongCodeResult = await new RegistrationController(world.Service).Confirm(
            new ConfirmRegistrationRequest
            {
                RegistrationId = started.RegistrationId,
                Code = wrongCode,
                Password = "A-good-password-1!"
            },
            default);
        var unknownRegistrationResult = await new RegistrationController(world.Service).Confirm(
            new ConfirmRegistrationRequest
            {
                RegistrationId = Guid.NewGuid(),
                Code = wrongCode,
                Password = "A-good-password-1!"
            },
            default);

        var wrongCodeRefusal = Assert.IsType<ObjectResult>(wrongCodeResult, exactMatch: false);
        var unknownRefusal = Assert.IsType<ObjectResult>(unknownRegistrationResult, exactMatch: false);
        Assert.Equal(wrongCodeRefusal.StatusCode, unknownRefusal.StatusCode);

        var wrongCodeProblem = Assert.IsType<ValidationProblemDetails>(wrongCodeRefusal.Value);
        var unknownProblem = Assert.IsType<ValidationProblemDetails>(unknownRefusal.Value);
        Assert.Equal(wrongCodeProblem.Status, unknownProblem.Status);
        Assert.Equal(wrongCodeProblem.Title, unknownProblem.Title);
        Assert.Equal(wrongCodeProblem.Type, unknownProblem.Type);
        Assert.Equal(
            wrongCodeProblem.Errors.OrderBy(error => error.Key).Select(error => (error.Key, string.Join("|", error.Value))),
            unknownProblem.Errors.OrderBy(error => error.Key).Select(error => (error.Key, string.Join("|", error.Value))));
    }

    [Fact]
    public async Task Confirm_AnsweringForAnAddressAlreadyClaimed_IsAGenericConflict()
    {
        var world = new RegistrationWorld();
        var controller = new RegistrationController(world.Service);
        var started = await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);
        var code = world.CodeFor(started.RegistrationId);

        // The address becomes an account before this attempt is confirmed.
        await world.CreateAccount("riya@example.test");

        var result = await controller.Confirm(
            new ConfirmRegistrationRequest
            {
                RegistrationId = started.RegistrationId,
                Code = code,
                Password = "A-good-password-1!"
            },
            default);

        Assert.IsType<ConflictResult>(result);
    }

    [Fact]
    public async Task Resend_ForAnUnknownRegistration_ReportsNothingSent()
    {
        var world = new RegistrationWorld();
        var controller = new RegistrationController(world.Service);

        var result = await controller.Resend(
            new ResendRegistrationRequest { RegistrationId = Guid.NewGuid() },
            default);

        var response = Assert.IsType<ResendRegistrationResponse>(
            Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.False(response.Sent);
        Assert.Equal(0, response.RetryAfterSeconds);
        Assert.Equal(0, response.ResendsLeft);
    }

    [Fact]
    public async Task Resend_ReturnsSentAndRetryAfterAndResendsLeft()
    {
        var world = new RegistrationWorld();
        var controller = new RegistrationController(world.Service);
        var started = await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);

        world.Clock.Advance(TimeSpan.FromSeconds(30));
        var result = await controller.Resend(
            new ResendRegistrationRequest { RegistrationId = started.RegistrationId },
            default);

        var response = Assert.IsType<ResendRegistrationResponse>(
            Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.True(response.Sent);
        Assert.Equal(30, response.RetryAfterSeconds);
        Assert.Equal(2, response.ResendsLeft);
    }

    [Fact]
    public async Task Cancel_AlwaysReturnsOk()
    {
        var world = new RegistrationWorld();
        var controller = new RegistrationController(world.Service);

        var result = await controller.Cancel(
            new CancelRegistrationRequest { Token = "not-a-real-token" },
            default);

        Assert.IsType<OkResult>(result);
    }
}
