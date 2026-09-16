using FacetIQ.Data.Context;
using FacetIQ.Data.DependencyInjection;
using FacetIQ.Data.Identity;
using FacetIQ.Data.Seeding;
using FacetIQ.Services.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// appsettings ships an empty value, so null alone is not enough.
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
}

builder.Services.AddDataLayer(connectionString);
builder.Services.AddServiceLayer();

builder.Services
    .AddIdentityApiEndpoints<AppUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AuthDbContext>();

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

// Bearer tokens, not cookies, so credentials stay off.
const string BrowserClients = "BrowserClients";

builder.Services.AddCors(options => options.AddPolicy(BrowserClients, policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();

    app.MapScalarApiReference(options =>
    {
        options.WithOpenApiRoutePattern("/openapi/v1.json");
        options.WithTheme(ScalarTheme.DeepSpace);
    }).AllowAnonymous();

    // Accounts for the seeded user ids. Migrations are not applied here.
    await app.Services.SeedDevelopmentUsersAsync();
}

// A redirect answers a preflight with a 307, which the browser will not follow.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Before authentication, or the anonymous preflight is refused by the fallback policy.
app.UseCors(BrowserClients);

app.UseAuthentication();
app.UseAuthorization();

app.MapIdentityApi<AppUser>().AllowAnonymous();

app.MapControllers();

app.Run();
