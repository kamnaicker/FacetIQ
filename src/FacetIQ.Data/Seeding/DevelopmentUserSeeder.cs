using FacetIQ.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace FacetIQ.Data.Seeding;

/// <summary>
/// Creates the accounts the seeded example is written about, carrying the identifiers the seed
/// rows already name. Nothing in the app schema is written and the operation is idempotent.
///
/// Development only: elsewhere the seeded subject stays unowned, since a demonstration profile
/// should not be claimable on a deployed instance. This seeds, it does not migrate.
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

            // Creation reports failure rather than throwing, and a silently empty user table is
            // a long hour of wondering why every sign-in is refused.
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not seed the development account '{email}': " +
                    string.Join("; ", result.Errors.Select(error => error.Description)));
            }
        }
    }
}
