using FacetIQ.Domain.Abstractions.Repositories;

namespace FacetIQ.Conformity.Tests.Gdpr;

internal sealed class InMemoryUserDirectory : IUserDirectory
{
    private readonly (string UserId, string Email)[] _accounts;

    public InMemoryUserDirectory(params (string UserId, string Email)[] accounts)
    {
        _accounts = accounts;
    }

    public Task<string?> FindUserIdByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var account = _accounts.FirstOrDefault(candidate => candidate.Email == email);

        return Task.FromResult<string?>(account.UserId);
    }

    public Task<string?> FindEmailAsync(string userId, CancellationToken cancellationToken)
    {
        var account = _accounts.FirstOrDefault(candidate => candidate.UserId == userId);

        return Task.FromResult<string?>(account.Email);
    }
}