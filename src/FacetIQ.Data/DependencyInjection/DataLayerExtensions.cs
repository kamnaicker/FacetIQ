using FacetIQ.Data.Context;
using FacetIQ.Data.Repositories;
using FacetIQ.Domain.Abstractions.Repositories;
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
                .MigrationsHistoryTable("__EFMigrationsHistory", "auth")
                .MigrationsAssembly(typeof(AuthDbContext).Assembly.FullName)));

        services.AddDbContext<FacetIQDbContext>(o =>
            o.UseNpgsql(connectionString, npg => npg
                .MigrationsHistoryTable("__EFMigrationsHistory", "app")
                .MigrationsAssembly(typeof(FacetIQDbContext).Assembly.FullName)));

        services.AddScoped<INormRepository, NormRepository>();
        services.AddScoped<ISubjectRepository, SubjectRepository>();
        services.AddScoped<IAttributeRepository, AttributeRepository>();
        services.AddScoped<IAuditRecordRepository, AuditRecordRepository>();

        return services;
    }
}
