using FacetIQ.Data.Identity;
using Microsoft.AspNetCore.Identity;

namespace FacetIQ.Conformity.Tests.Gdpr;

/// <summary>
/// Lets a test construct a UserManager whose lookups it overrides. Nothing else on it is ever
/// called, and anything that is fails loudly.
/// </summary>
internal sealed class UnusedUserStore : IUserStore<AppUser>
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
