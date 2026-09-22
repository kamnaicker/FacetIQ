using System.Security.Claims;
using FacetIQ.API.Account;
using FacetIQ.Contracts.Account;
using Microsoft.AspNetCore.Mvc;

namespace FacetIQ.API.Controllers;

[ApiController]
[Route("account")]
public class AccountController : ControllerBase
{
    private readonly AccountDeletion _deletion;

    public AccountController(AccountDeletion deletion)
    {
        _deletion = deletion;
    }

    /// <summary>Deletes the caller's account. The disclosure log is kept, but nothing in it identifies them afterwards.</summary>
    [HttpDelete]
    public async Task<ActionResult> Delete([FromBody] DeleteAccountRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
        {
            return Unauthorized();
        }

        var outcome = await _deletion.DeleteAsync(userId, request.Password, cancellationToken);

        if (outcome == DeletionOutcome.Deleted)
        {
            return NoContent();
        }

        if (outcome == DeletionOutcome.WrongPassword)
        {
            ModelState.AddModelError(nameof(request.Password), "The password is not right.");

            return ValidationProblem(statusCode: StatusCodes.Status400BadRequest, modelStateDictionary: ModelState);
        }

        if (outcome == DeletionOutcome.LockedOut)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests);
        }

        // A token outliving its account: the session is over.
        if (outcome == DeletionOutcome.NotFound)
        {
            return Unauthorized();
        }

        return Problem(statusCode: StatusCodes.Status500InternalServerError);
    }
}
