using ElsasaSis.Contracts.Student;
using ElsasaSis.Modules.Std.Domain;
using ElsasaSis.Modules.Std.Infrastructure.Contracts;
using ElsasaSis.Modules.Std.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ElsasaSis.Modules.Std;

public static class DependencyInjection
{
    public static IServiceCollection AddStdModule(this IServiceCollection services)
    {
        services.AddScoped<ICandidateRepository, CandidateRepository>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<IStudentEnrollmentRepository, StudentEnrollmentRepository>();
        services.AddScoped<IStudyGroupRepository, StudyGroupRepository>();
        services.AddScoped<IStudentPlacementRepository, StudentPlacementRepository>();

        services.AddScoped<IStudentContract, StudentContract>();

        RegisterDiscoveredHandlersAndValidators(services);

        return services;
    }

    private static void RegisterDiscoveredHandlersAndValidators(IServiceCollection services)
    {
        var assembly = typeof(StdAssemblyMarker).Assembly;

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
