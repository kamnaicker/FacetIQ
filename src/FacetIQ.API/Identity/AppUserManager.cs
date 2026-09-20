using FacetIQ.Data.Identity;
using FacetIQ.Services.Subjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace FacetIQ.API.Identity;

// Identity's /confirmEmail goes through UserManager, so a confirmed account gets its subject here.
public sealed class AppUserManager : UserManager<AppUser>
{
    private readonly SubjectProvisioner _provisioner;

    public AppUserManager(
        IUserStore<AppUser> store,
        IOptions<IdentityOptions> optionsAccessor,
        IPasswordHasher<AppUser> passwordHasher,
        IEnumerable<IUserValidator<AppUser>> userValidators,
        IEnumerable<IPasswordValidator<AppUser>> passwordValidators,
        ILookupNormalizer keyNormalizer,
        IdentityErrorDescriber errors,
        IServiceProvider services,
        ILogger<UserManager<AppUser>> logger,
        SubjectProvisioner provisioner)
        : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
    {
        _provisioner = provisioner;
    }

    public override async Task<IdentityResult> ConfirmEmailAsync(AppUser user, string token)
    {
        var result = await base.ConfirmEmailAsync(user, token);

        if (result.Succeeded)
        {
            try
            {
                await _provisioner.EnsureAsync(user.Id, CancellationToken.None);
            }
            catch (Exception exception)
            {
                // The account is confirmed either way. POST /subject creates the profile on sign in,
                // so a failure here must not turn a working link into an error page.
                Logger.LogWarning(exception, "Could not create the profile for a confirmed account.");
            }
        }

        return result;
    }
}
