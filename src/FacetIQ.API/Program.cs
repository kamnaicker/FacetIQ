using System.Threading.RateLimiting;
using FacetIQ.API.Email;
using FacetIQ.API.OpenApi;
using FacetIQ.API.RateLimiting;
using FacetIQ.Data.Context;
using FacetIQ.Data.DependencyInjection;
using FacetIQ.Data.Identity;
using FacetIQ.Data.Seeding;
using FacetIQ.Domain.Abstractions.Repositories;
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
    .AddIdentityApiEndpoints<AppUser>(options =>
    {
        // Lookups and standings key on email, so an address must be proven before it counts.
        options.SignIn.RequireConfirmedEmail = true;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AuthDbContext>();

builder.Services.AddScoped<IUserDirectory, IdentityUserDirectory>();

builder.Services
    .AddOptions<SmtpOptions>()
    .Bind(builder.Configuration.GetSection(SmtpOptions.Section))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Singletons: MapIdentityApi resolves the sender once, from the root provider.
builder.Services.AddSingleton<IMailTransport, SmtpMailTransport>();
builder.Services.AddSingleton<RecipientThrottle>();
builder.Services.AddSingleton<IEmailSender<AppUser>, IdentityEmailSender>();

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

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(AccountEmailRateLimit.Partition);
});

builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();

    app.MapScalarApiReference(options =>
    {
        options.WithOpenApiRoutePattern("/openapi/v1.json");
        options.WithTheme(ScalarTheme.DeepSpace);
        options.AddPreferredSecuritySchemes("Bearer");
    }).AllowAnonymous();

    // Accounts for the seeded user ids. Migrations are not applied here.
    var seedPassword = app.Configuration["Seed:Password"];

    if (string.IsNullOrWhiteSpace(seedPassword))
    {
        throw new InvalidOperationException(
            "Seed:Password is not configured. Set it with dotnet user-secrets.");
    }

    await app.Services.SeedDevelopmentUsersAsync(seedPassword);
}

// A redirect answers a preflight with a 307, which the browser will not follow.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Before authentication, or the anonymous preflight is refused by the fallback policy.
app.UseCors(BrowserClients);

// After CORS, so a refused request still carries the headers a browser needs to read it.
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapIdentityApi<AppUser>().AllowAnonymous();

app.MapControllers();

app.Run();
