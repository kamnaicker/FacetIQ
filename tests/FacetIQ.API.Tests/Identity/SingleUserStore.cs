using FacetIQ.Data.Identity;
using Microsoft.AspNetCore.Identity;

namespace FacetIQ.API.Tests.Identity;

// Holds one account in memory. UserManager needs a store; only the email parts are exercised here.
internal sealed class SingleUserStore : IUserStore<AppUser>, IUserEmailStore<AppUser>
{
    private readonly AppUser _user;

    public SingleUserStore(AppUser user)
    {
        _user = user;
    }

    public Task<string> GetUserIdAsync(AppUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Id);
    }

    public Task<string?> GetUserNameAsync(AppUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.UserName);
    }

    public Task SetUserNameAsync(AppUser user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;

        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(AppUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.NormalizedUserName);
    }

    public Task SetNormalizedUserNameAsync(AppUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;

        return Task.CompletedTask;
    }

    public Task<IdentityResult> CreateAsync(AppUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IdentityResult> UpdateAsync(AppUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IdentityResult> DeleteAsync(AppUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<AppUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_user.Id == userId ? _user : null);
    }

    public Task<AppUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        return Task.FromResult(_user.NormalizedUserName == normalizedUserName ? _user : null);
    }

    public Task SetEmailAsync(AppUser user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email;

        return Task.CompletedTask;
    }

    public Task<string?> GetEmailAsync(AppUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Email);
    }

    public Task<bool> GetEmailConfirmedAsync(AppUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.EmailConfirmed);
    }

    public Task SetEmailConfirmedAsync(AppUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.EmailConfirmed = confirmed;

        return Task.CompletedTask;
    }

    public Task<AppUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return Task.FromResult(_user.NormalizedEmail == normalizedEmail ? _user : null);
    }

    public Task<string?> GetNormalizedEmailAsync(AppUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.NormalizedEmail);
    }

    public Task SetNormalizedEmailAsync(AppUser user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        user.NormalizedEmail = normalizedEmail;

        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }
}
