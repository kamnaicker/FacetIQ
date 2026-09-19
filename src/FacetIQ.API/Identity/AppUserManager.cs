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
            await _provisioner.EnsureAsync(user.Id, CancellationToken.None);
        }

        return result;
    }
}
