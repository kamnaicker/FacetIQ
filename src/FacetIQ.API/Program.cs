using FacetIQ.API.Authorization;
using FacetIQ.Data.Context;
using FacetIQ.Data.DependencyInjection;
using FacetIQ.Data.Identity;
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

builder.Services
    .AddIdentityApiEndpoints<AppUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AuthDbContext>();

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

builder.Services.AddScoped<IAuthorizationHandler, RequesterClaimsHandler>();

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
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapIdentityApi<AppUser>();

app.MapControllers();

app.Run();
