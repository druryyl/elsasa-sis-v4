using ElsasaSis.Contracts.Development;
using ElsasaSis.Modules.Dev.Domain;
using ElsasaSis.Modules.Dev.Infrastructure.Contracts;
using ElsasaSis.Modules.Dev.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ElsasaSis.Modules.Dev;

public static class DependencyInjection
{
    public static IServiceCollection AddDevModule(this IServiceCollection services)
    {
        services.AddScoped<IDevelopmentModule, DevelopmentModuleContract>();

        services.AddScoped<IBehaviourNoteRepository, BehaviourNoteRepository>();
        services.AddScoped<IDisciplineRecordRepository, DisciplineRecordRepository>();
        services.AddScoped<IExtracurricularActivityRepository, ExtracurricularActivityRepository>();
        services.AddScoped<IAchievementRepository, AchievementRepository>();

        RegisterDiscoveredHandlersAndValidators(services);

        return services;
    }

    private static void RegisterDiscoveredHandlersAndValidators(IServiceCollection services)
    {
        var assembly = typeof(DevAssemblyMarker).Assembly;

        foreach (var type in assembly.DefinedTypes)
        {
            if (!type.IsClass || type.IsAbstract || type.IsGenericTypeDefinition)
            {
                continue;
            }

            if (type.Name.EndsWith("Handler", StringComparison.Ordinal))
            {
                services.AddScoped(type.AsType());
            }

            foreach (var implementedInterface in type.ImplementedInterfaces)
            {
                if (implementedInterface.IsGenericType &&
                    implementedInterface.GetGenericTypeDefinition() == typeof(IValidator<>))
                {
                    services.AddScoped(implementedInterface, type.AsType());
                }
            }
        }
    }
}
