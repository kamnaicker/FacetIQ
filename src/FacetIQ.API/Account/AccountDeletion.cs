using FacetIQ.Data.Identity;
using FacetIQ.Domain.Abstractions.Repositories;
using Microsoft.AspNetCore.Identity;

namespace FacetIQ.API.Account;

public enum DeletionOutcome
{
    Deleted,
    WrongPassword,
    LockedOut,
    NotFound,
    Failed
}

/// <summary>Deletes an account once its password is confirmed.</summary>
public sealed class AccountDeletion
{
    private readonly UserManager<AppUser> _users;
    private readonly IAccountDataEraser _eraser;
    private readonly ILogger<AccountDeletion> _logger;

    public AccountDeletion(UserManager<AppUser> users, IAccountDataEraser eraser, ILogger<AccountDeletion> logger)
    {
        _users = users;
        _eraser = eraser;
        _logger = logger;
    }

    public async Task<DeletionOutcome> DeleteAsync(string userId, string password, CancellationToken cancellationToken)
    {
        var user = await _users.FindByIdAsync(userId);

        if (user is null)
        {
            return DeletionOutcome.NotFound;
        }

        if (await _users.IsLockedOutAsync(user))
        {
            return DeletionOutcome.LockedOut;
        }

        // Counted towards lockout, so a stolen session cannot be used to guess the password here.
        if (!await _users.CheckPasswordAsync(user, password))
        {
            await _users.AccessFailedAsync(user);

            return DeletionOutcome.WrongPassword;
        }

        // Data before login: if the login deletion then fails, signing in again only finds an empty
        // profile. The other order could leave data behind that nobody can reach to delete.
        await _eraser.EraseAsync(userId, cancellationToken);

        var deleted = await _users.DeleteAsync(user);

        if (!deleted.Succeeded)
        {
            _logger.LogError(
                "Account data was erased but the login could not be deleted: {Errors}",
                string.Join(", ", deleted.Errors.Select(error => error.Code)));

            return DeletionOutcome.Failed;
        }

        return DeletionOutcome.Deleted;
    }
}
