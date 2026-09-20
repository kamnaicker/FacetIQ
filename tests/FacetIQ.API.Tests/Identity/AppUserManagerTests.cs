using FacetIQ.API.Identity;
using FacetIQ.Data.Identity;
using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;
using FacetIQ.Services.Subjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FacetIQ.API.Tests.Identity;

/// <summary>The only place a subject is created without an authenticated caller.</summary>
public class AppUserManagerTests
{
    private const string GoodToken = "good";

    [Fact]
    public async Task ConfirmedEmail_GivesTheAccountItsSubject()
    {
        var user = Account();
        var subjects = new InMemorySubjects();

        var result = await ManagerFor(user, subjects).ConfirmEmailAsync(user, GoodToken);

        Assert.True(result.Succeeded);
        Assert.True(user.EmailConfirmed);
        Assert.Equal(user.Id, Assert.Single(subjects.Rows).UserId);
    }

    [Fact]
    public async Task RefusedToken_LeavesNoSubject()
    {
        var user = Account();
        var subjects = new InMemorySubjects();

        var result = await ManagerFor(user, subjects).ConfirmEmailAsync(user, "wrong");

        Assert.False(result.Succeeded);
        Assert.False(user.EmailConfirmed);
        Assert.Empty(subjects.Rows);
    }

    /// <summary>A second click on the same link must not make a second profile.</summary>
    [Fact]
    public async Task ConfirmingTwice_KeepsOneSubject()
    {
        var user = Account();
        var subjects = new InMemorySubjects();
        var manager = ManagerFor(user, subjects);

        await manager.ConfirmEmailAsync(user, GoodToken);
        await manager.ConfirmEmailAsync(user, GoodToken);

        Assert.Single(subjects.Rows);
    }

    /// <summary>The account is confirmed either way; POST /subject creates the profile on sign in.</summary>
    [Fact]
    public async Task SubjectThatCannotBeStored_StillConfirmsTheAccount()
    {
        var user = Account();

        var result = await ManagerFor(user, new FailingSubjects()).ConfirmEmailAsync(user, GoodToken);

        Assert.True(result.Succeeded);
        Assert.True(user.EmailConfirmed);
    }

    private static AppUser Account()
    {
        return new AppUser
        {
            Id = "user-1",
            UserName = "riya@example.test",
            Email = "riya@example.test",
        };
    }

    private static AppUserManager ManagerFor(AppUser user, ISubjectRepository subjects)
    {
        var options = Options.Create(new IdentityOptions());

        var manager = new AppUserManager(
            new SingleUserStore(user),
            options,
            new PasswordHasher<AppUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            services: null!,
            NullLogger<UserManager<AppUser>>.Instance,
            new SubjectProvisioner(subjects));

        manager.RegisterTokenProvider(options.Value.Tokens.EmailConfirmationTokenProvider, new OneTokenProvider());

        return manager;
    }

    // Accepts one token, so a confirmation can be exercised without Data Protection.
    private sealed class OneTokenProvider : IUserTwoFactorTokenProvider<AppUser>
    {
        public Task<string> GenerateAsync(string purpose, UserManager<AppUser> manager, AppUser user)
        {
            return Task.FromResult(GoodToken);
        }

        public Task<bool> ValidateAsync(string purpose, string token, UserManager<AppUser> manager, AppUser user)
        {
            return Task.FromResult(token == GoodToken);
        }

        public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<AppUser> manager, AppUser user)
        {
            return Task.FromResult(false);
        }
    }

    private sealed class InMemorySubjects : ISubjectRepository
    {
        public List<Subject> Rows { get; } = [];

        public Task<Subject?> FindAsync(Guid subjectId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Rows.SingleOrDefault(subject => subject.Id == subjectId));
        }

        public Task<Subject?> FindByUserIdAsync(string userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Rows.SingleOrDefault(subject => subject.UserId == userId));
        }

        public Task<Subject> AddOrGetAsync(Subject subject, CancellationToken cancellationToken)
        {
            Rows.Add(subject);

            return Task.FromResult(subject);
        }
    }

    private sealed class FailingSubjects : ISubjectRepository
    {
        public Task<Subject?> FindAsync(Guid subjectId, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("The database is unreachable.");
        }

        public Task<Subject?> FindByUserIdAsync(string userId, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("The database is unreachable.");
        }

        public Task<Subject> AddOrGetAsync(Subject subject, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("The database is unreachable.");
        }
    }
}
