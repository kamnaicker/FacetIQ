using FacetIQ.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FacetIQ.Data.DependencyInjection;

public static class DataLayerExtensions
{
    public static IServiceCollection AddDataLayer(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<AuthDbContext>(o =>
            o.UseNpgsql(connectionString, npg => npg
                .MigrationsHistoryTable("__EFMigrationHistory", "auth")
                .MigrationsAssembly(typeof(AuthDbContext).Assembly.FullName)));

        services.AddDbContext<FacetIQDbContext>(o =>
            o.UseNpgsql(connectionString, npg => npg
                .MigrationsHistoryTable("__EFMigrationsHistory", "app")));

        return services;
    }
}
