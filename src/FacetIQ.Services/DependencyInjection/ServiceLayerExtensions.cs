using FacetIQ.Domain.Abstractions.Services;
using FacetIQ.Services.Auditing;
using FacetIQ.Services.Authoring;
using FacetIQ.Services.Disclosure;
using FacetIQ.Services.Matching;
using FacetIQ.Services.Subjects;
using FacetIQ.Services.Transformation;
using FacetIQ.Services.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace FacetIQ.Services.DependencyInjection;

public static class ServiceLayerExtensions
{
    public static IServiceCollection AddServiceLayer(this IServiceCollection services)
    {
        // Stateless and storage-free, so singletons.
        services.AddSingleton<INormMatcher, NormMatcher>();
        services.AddSingleton<ISpecificityRanker, SpecificityRanker>();
        services.AddSingleton<ITransformService, TransformService>();
        services.AddSingleton<IConflictDetector, ConflictDetector>();
        services.AddSingleton<IClaimValueValidator, ClaimValueValidator>();
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<IDisclosureEvaluator, DisclosureEvaluator>();
        services.AddScoped<SubjectProvisioner>();

        return services;
    }
}
