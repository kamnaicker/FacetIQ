using FacetIQ.API.Identity;
using FacetIQ.Data.Context;
using FacetIQ.Data.DependencyInjection;
using FacetIQ.Data.Identity;
using FacetIQ.Services.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace FacetIQ.API.Tests.Identity;

/// <summary>Identity's endpoints resolve UserManager, so the subclass has to be the one they get.</summary>
public class IdentityRegistrationTests
{
    [Fact]
    public void UserManager_ResolvesToTheSubclassThatProvisionsSubjects()
    {
        var services = new ServiceCollection();

        // No connection is opened; the container only has to build.
        services.AddLogging();
        services.AddDataLayer("Host=localhost;Database=facetiq;Username=none;Password=none");
        services.AddServiceLayer();

        services
            .AddIdentityApiEndpoints<AppUser>()
            .AddRoles<IdentityRole>()
            .AddUserManager<AppUserManager>()
            .AddEntityFrameworkStores<AuthDbContext>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<AppUserManager>(scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>());
    }
}
