using FacetIQ.Domain.Abstractions.Services;
using FacetIQ.Services.Auditing;
using FacetIQ.Services.Disclosure;
using FacetIQ.Services.Matching;
using FacetIQ.Services.Transformation;
using Microsoft.Extensions.DependencyInjection;

namespace FacetIQ.Services.DependencyInjection;

public static class ServiceLayerExtensions
{
    public static IServiceCollection AddServiceLayer(this IServiceCollection services)
    {
        // Matching, ranking and transformation hold no state and touch no storage, so a
        // single instance serves every request.
        services.AddSingleton<INormMatcher, NormMatcher>();
        services.AddSingleton<ISpecificityRanker, SpecificityRanker>();
        services.AddSingleton<ITransformService, TransformService>();
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<IDisclosureEvaluator, DisclosureEvaluator>();

        return services;
    }
}
