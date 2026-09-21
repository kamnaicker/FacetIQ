using FacetIQ.API.Registration;
using FacetIQ.Contracts.Registration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacetIQ.API.Controllers;

[ApiController]
[Route("registration")]
[AllowAnonymous]
public class RegistrationController : ControllerBase
{
    private readonly RegistrationService _registrations;

    public RegistrationController(RegistrationService registrations)
    {
        _registrations = registrations;
    }

    /// <summary>Starts a registration: writes a pending row and emails a code.</summary>
    [HttpPost]
    public async Task<ActionResult<StartRegistrationResponse>> Post(
        StartRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _registrations.StartAsync(request.Email, request.Password, cancellationToken);

        if (result.Outcome == StartOutcome.CodeSent)
        {
            return Ok(new StartRegistrationResponse { RegistrationId = result.RegistrationId });
        }

        if (result.Outcome == StartOutcome.AlreadyRegistered)
        {
            // No body: the holder of the address is told by email instead.
            return Conflict();
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Code, error.Description);
        }

        return ValidationProblem(statusCode: StatusCodes.Status400BadRequest, modelStateDictionary: ModelState);
    }

    /// <summary>Redeems the code and creates the account. No session is returned; sign in through /login.</summary>
    [HttpPost("confirm")]
    public async Task<ActionResult> Confirm(ConfirmRegistrationRequest request, CancellationToken cancellationToken)
    {
        var outcome = await _registrations.ConfirmAsync(
            request.RegistrationId,
            request.Code,
            request.Password,
            cancellationToken);

        if (outcome == ConfirmOutcome.Created)
        {
            return NoContent();
        }

        if (outcome == ConfirmOutcome.AlreadyRegistered)
        {
            return Conflict();
        }

        if (outcome == ConfirmOutcome.Failed)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError);
        }

        // A wrong code, an unknown id and an expired attempt get the same answer, so none can be probed.
        ModelState.AddModelError(nameof(request.Code), "The code is invalid or has expired.");

        return ValidationProblem(statusCode: StatusCodes.Status400BadRequest, modelStateDictionary: ModelState);
    }

    /// <summary>Sends a fresh code. The registration id is unguessable, so answering truthfully reveals nothing.</summary>
    [HttpPost("resend")]
    public async Task<ActionResult<ResendRegistrationResponse>> Resend(
        ResendRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _registrations.ResendAsync(request.RegistrationId, cancellationToken);

        return Ok(new ResendRegistrationResponse
        {
            Sent = result.Sent,
            RetryAfterSeconds = result.RetryAfterSeconds,
            ResendsLeft = result.ResendsLeft
        });
    }

    /// <summary>Removes every pending attempt for the address. Answers the same whether or not the token exists.</summary>
    [HttpPost("cancel")]
    public async Task<ActionResult> Cancel(CancelRegistrationRequest request, CancellationToken cancellationToken)
    {
        await _registrations.CancelAsync(request.Token, cancellationToken);

        return Ok();
    }
}
