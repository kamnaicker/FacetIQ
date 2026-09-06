using FacetIQ.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace FacetIQ.Data.Seeding;

/// <summary>
/// Creates the accounts the seeded worked example is written about.
///
/// The seeded rows already name their people by user identifier, so rather than rebinding those
/// rows to whatever ids Identity would generate, the accounts are created carrying the identifiers
/// the seed already uses. Nothing in the app schema is written, the operation is idempotent, and a
/// database dropped and rebuilt from migrations comes back consistent without further work.
///
/// This seeds; it does not migrate. Schema changes are still applied deliberately from a command
/// line, so a bad deployment cannot alter a database on startup.
///
/// Development only. Anywhere else the seeded subject stays unowned, which is correct: a
/// demonstration profile should not be claimable on a deployed instance.
/// </summary>
public static class DevelopmentUserSeeder
{
    private const string Password = "Passw0rd!";

    private static readonly (string Id, string Email)[] Accounts =
    [
        (SeedData.AmaraUserId, "amara@example.test"),
        (SeedData.AcceptedColleagueUserId, "colleague.accepted@example.test"),
        (SeedData.PendingColleagueUserId, "colleague.pending@example.test")
    ];

    public static async Task SeedDevelopmentUsersAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        foreach (var (id, email) in Accounts)
        {
            if (await users.FindByIdAsync(id) is not null)
            {
                continue;
            }

            var user = new AppUser
            {
                Id = id,
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await users.CreateAsync(user, Password);

            // Creation reports failure rather than throwing it, and an empty user table that
            // nothing complained about is a long hour of wondering why every sign-in is refused.
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not seed the development account '{email}': " +
                    string.Join("; ", result.Errors.Select(error => error.Description)));
            }
        }
    }
}
