using FacetIQ.Data.Identity;
using Microsoft.AspNetCore.Identity;

namespace FacetIQ.Data.Tests.Identity;

public class IdentityUserDirectoryTests
{
    private static readonly AppUser Riya = new() { Id = "riya", Email = "riya@example.test" };

    [Fact]
    public async Task KnownEmail_ReturnsUserId()
    {
        var userId = await Directory().FindUserIdByEmailAsync("riya@example.test", CancellationToken.None);

        Assert.Equal("riya", userId);
    }

    [Fact]
    public async Task UnknownEmail_ReturnsNull()
    {
        var userId = await Directory().FindUserIdByEmailAsync("nobody@example.test", CancellationToken.None);

        Assert.Null(userId);
    }

    [Fact]
    public async Task KnownUserId_ReturnsEmail()
    {
        var email = await Directory().FindEmailAsync("riya", CancellationToken.None);

        Assert.Equal("riya@example.test", email);
    }

    [Fact]
    public async Task UnknownUserId_ReturnsNull()
    {
        var email = await Directory().FindEmailAsync("deleted-account", CancellationToken.None);

        Assert.Null(email);
    }

    private static IdentityUserDirectory Directory()
    {
        return new IdentityUserDirectory(new SeededUserManager(Riya));
    }

    private sealed class SeededUserManager : UserManager<AppUser>
    {
        private readonly AppUser[] _accounts;

        public SeededUserManager(params AppUser[] accounts)
            : base(new EmptyUserStore(), null!, null!, null!, null!, null!, null!, null!, null!)
        {
            _accounts = accounts;
        }

        public override Task<AppUser?> FindByEmailAsync(string email)
        {
            return Task.FromResult(_accounts.SingleOrDefault(account => account.Email == email));
        }

        public override Task<AppUser?> FindByIdAsync(string userId)
        {
            return Task.FromResult(_accounts.SingleOrDefault(account => account.Id == userId));
        }
    }
}