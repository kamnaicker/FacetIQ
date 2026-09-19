using FacetIQ.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace FacetIQ.Data.Seeding;

/// <summary>
/// Creates Identity accounts with the user ids SeedData uses. Idempotent. Development only, so
/// the seeded profile cannot be signed into on a deployed instance.
/// </summary>
public static class DevelopmentUserSeeder
{
    private static readonly (string Id, string Email)[] Accounts =
    [
        (SeedData.AmaraUserId, "amara@example.test"),
        (SeedData.AcceptedColleagueUserId, "colleague.accepted@example.test"),
        (SeedData.PendingColleagueUserId, "colleague.pending@example.test")
    ];

    /// <summary>The password comes from user secrets, so no credential is kept in the repository.</summary>
    public static async Task SeedDevelopmentUsersAsync(this IServiceProvider services, string password)
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

            var result = await users.CreateAsync(user, password);

            // CreateAsync reports failure instead of throwing.
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not seed the development account '{email}': " +
                    string.Join("; ", result.Errors.Select(error => error.Description)));
            }
        }
    }
}
