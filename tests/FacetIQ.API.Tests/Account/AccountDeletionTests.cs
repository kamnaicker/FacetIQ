using System.Security.Claims;
using FacetIQ.API.Account;
using FacetIQ.API.Controllers;
using FacetIQ.Contracts.Account;
using FacetIQ.Data.Identity;
using FacetIQ.Domain.Abstractions.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FacetIQ.API.Tests.Account;

public class AccountDeletionTests
{
    private const string Password = "A-good-password-1!";

    [Fact]
    public async Task RightPassword_ErasesTheDataAndDeletesTheLogin()
    {
        var world = new DeletionWorld();

        var outcome = await world.Deletion.DeleteAsync(world.UserId, Password, default);

        Assert.Equal(DeletionOutcome.Deleted, outcome);
        Assert.Equal([world.UserId], world.Eraser.Erased);
        Assert.Empty(world.Store.Users);
    }

    [Fact]
    public async Task DataIsErasedWhileTheLoginStillExists()
    {
        var world = new DeletionWorld();

        await world.Deletion.DeleteAsync(world.UserId, Password, default);

        Assert.True(world.Eraser.LoginExistedWhenErased);
    }

    [Fact]
    public async Task WrongPassword_TouchesNothingAndCountsAFailure()
    {
        var world = new DeletionWorld();

        var outcome = await world.Deletion.DeleteAsync(world.UserId, "Not-the-password-1!", default);

        Assert.Equal(DeletionOutcome.WrongPassword, outcome);
        Assert.Empty(world.Eraser.Erased);
        Assert.Equal(1, Assert.Single(world.Store.Users).AccessFailedCount);
    }

    [Fact]
    public async Task FifthWrongPassword_LocksTheAccountAndTheRightOneIsThenRefused()
    {
        var world = new DeletionWorld();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await world.Deletion.DeleteAsync(world.UserId, "Not-the-password-1!", default);
        }

        var outcome = await world.Deletion.DeleteAsync(world.UserId, Password, default);

        Assert.Equal(DeletionOutcome.LockedOut, outcome);
        Assert.Empty(world.Eraser.Erased);
        Assert.Single(world.Store.Users);
    }

    [Fact]
    public async Task UnknownAccount_TouchesNothing()
    {
        var world = new DeletionWorld();

        var outcome = await world.Deletion.DeleteAsync("no-such-user", Password, default);

        Assert.Equal(DeletionOutcome.NotFound, outcome);
        Assert.Empty(world.Eraser.Erased);
    }

    [Fact]
    public async Task Endpoint_RightPassword_IsNoContent()
    {
        var world = new DeletionWorld();

        var result = await world.Controller().Delete(new DeleteAccountRequest { Password = Password }, default);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Endpoint_WrongPassword_IsAValidationProblemOnThePassword()
    {
        var world = new DeletionWorld();

        var result = await world.Controller().Delete(
            new DeleteAccountRequest { Password = "Not-the-password-1!" },
            default);

        var refusal = Assert.IsType<ObjectResult>(result, exactMatch: false);
        var problem = Assert.IsType<ValidationProblemDetails>(refusal.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.True(problem.Errors.ContainsKey(nameof(DeleteAccountRequest.Password)));
    }

    [Fact]
    public async Task Endpoint_WhenLockedOut_IsTooManyRequests()
    {
        var world = new DeletionWorld();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await world.Deletion.DeleteAsync(world.UserId, "Not-the-password-1!", default);
        }

        var result = await world.Controller().Delete(new DeleteAccountRequest { Password = Password }, default);

        Assert.Equal(StatusCodes.Status429TooManyRequests, Assert.IsType<StatusCodeResult>(result).StatusCode);
    }

    private sealed class DeletionWorld
    {
        public AccountController Controller()
        {
            var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, UserId)], "Test");

            return new AccountController(Deletion)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
                }
            };
        }

        public DeletionWorld()
        {
            Store = new PasswordUserStore();

            var users = new UserManager<AppUser>(
                Store,
                Options.Create(new IdentityOptions()),
                new PasswordHasher<AppUser>(),
                [],
                [],
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                services: null!,
                NullLogger<UserManager<AppUser>>.Instance);

            var user = new AppUser
            {
                Id = UserId,
                UserName = "riya@example.test",
                Email = "riya@example.test",
                LockoutEnabled = true
            };
            user.PasswordHash = users.PasswordHasher.HashPassword(user, Password);
            Store.Users.Add(user);

            Eraser = new RecordingEraser(Store);
            Deletion = new AccountDeletion(users, Eraser, NullLogger<AccountDeletion>.Instance);
        }

        public string UserId { get; } = Guid.NewGuid().ToString();

        public PasswordUserStore Store { get; }

        public RecordingEraser Eraser { get; }

        public AccountDeletion Deletion { get; }
    }

    private sealed class RecordingEraser : IAccountDataEraser
    {
        private readonly PasswordUserStore _store;

        public RecordingEraser(PasswordUserStore store)
        {
            _store = store;
        }

        public List<string> Erased { get; } = [];

        public bool LoginExistedWhenErased { get; private set; }

        public Task EraseAsync(string userId, CancellationToken cancellationToken)
        {
            Erased.Add(userId);
            LoginExistedWhenErased = _store.Users.Any(user => user.Id == userId);

            return Task.CompletedTask;
        }
    }

    private sealed class PasswordUserStore :
        IUserStore<AppUser>,
        IUserPasswordStore<AppUser>,
        IUserLockoutStore<AppUser>
    {
        public List<AppUser> Users { get; } = [];

        public void Dispose()
        {
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
            Users.Add(user);

            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> UpdateAsync(AppUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> DeleteAsync(AppUser user, CancellationToken cancellationToken)
        {
            Users.Remove(user);

            return Task.FromResult(IdentityResult.Success);
        }

        public Task<AppUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Users.FirstOrDefault(user => user.Id == userId));
        }

        public Task<AppUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            return Task.FromResult(Users.FirstOrDefault(user => user.NormalizedUserName == normalizedUserName));
        }

        public Task SetPasswordHashAsync(AppUser user, string? passwordHash, CancellationToken cancellationToken)
        {
            user.PasswordHash = passwordHash;

            return Task.CompletedTask;
        }

        public Task<string?> GetPasswordHashAsync(AppUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.PasswordHash);
        }

        public Task<bool> HasPasswordAsync(AppUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.PasswordHash is not null);
        }

        public Task<DateTimeOffset?> GetLockoutEndDateAsync(AppUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.LockoutEnd);
        }

        public Task SetLockoutEndDateAsync(AppUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
        {
            user.LockoutEnd = lockoutEnd;

            return Task.CompletedTask;
        }

        public Task<int> IncrementAccessFailedCountAsync(AppUser user, CancellationToken cancellationToken)
        {
            user.AccessFailedCount += 1;

            return Task.FromResult(user.AccessFailedCount);
        }

        public Task ResetAccessFailedCountAsync(AppUser user, CancellationToken cancellationToken)
        {
            user.AccessFailedCount = 0;

            return Task.CompletedTask;
        }

        public Task<int> GetAccessFailedCountAsync(AppUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.AccessFailedCount);
        }

        public Task<bool> GetLockoutEnabledAsync(AppUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.LockoutEnabled);
        }

        public Task SetLockoutEnabledAsync(AppUser user, bool enabled, CancellationToken cancellationToken)
        {
            user.LockoutEnabled = enabled;

            return Task.CompletedTask;
        }
    }
}
