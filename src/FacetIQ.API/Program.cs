using FacetIQ.API.Authorization;
using FacetIQ.Data.Context;
using FacetIQ.Data.DependencyInjection;
using FacetIQ.Data.Identity;
using FacetIQ.Data.Seeding;
using FacetIQ.Services.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

string cs = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new 
    InvalidOperationException("Connection string 'FacetIQ' is not configured.");

// Add services to the container.
builder.Services.AddDataLayer(cs);
builder.Services.AddServiceLayer();

builder.Services
    .AddIdentityApiEndpoints<AppUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AuthDbContext>();

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

builder.Services.AddScoped<IAuthorizationHandler, RequesterClaimsHandler>();

// Origins come from configuration, so a new address is a setting rather than a rebuild. No
// credentials: the client sends a bearer token in a header, not a cookie.
const string BrowserClients = "BrowserClients";

builder.Services.AddCors(options => options.AddPolicy(BrowserClients, policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();

    app.MapScalarApiReference(options =>
    {
        // Tell Scalar where to find Microsoft's OpenAPI JSON file
        options.WithOpenApiRoutePattern("/openapi/v1.json");

        // Optional custom styling configuration
        options.WithTheme(ScalarTheme.DeepSpace);
    }).AllowAnonymous();

    // The seeded worked example names its people by user identifier; these are the accounts that
    // bear them, so the example can be signed into rather than only read about. Seeding only:
    // migrations are still applied deliberately, never on startup.
    await app.Services.SeedDevelopmentUsersAsync();
}

// In development the client calls over http, and a redirect answers preflight with a 307 the
// browser will not follow.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Before authentication: a preflight carries no credentials, so placed after it arrives anonymous
// and the fallback policy refuses it.
app.UseCors(BrowserClients);

// Authentication populates the principal that authorization then evaluates. The order is
// load-bearing: reversed, every request is anonymous and the fallback policy refuses it.
app.UseAuthentication();
app.UseAuthorization();

// Credentials cannot be required to obtain credentials, so these endpoints opt out of the
// fallback policy.
app.MapIdentityApi<AppUser>().AllowAnonymous();

app.MapControllers();

app.Run();
