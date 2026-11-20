using ElsasaSis.Contracts.Academic;
using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.Modules.Acd.Infrastructure.Contracts;
using ElsasaSis.Modules.Acd.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ElsasaSis.Modules.Acd;

public static class DependencyInjection
{
    public static IServiceCollection AddAcdModule(this IServiceCollection services)
    {
        services.AddScoped<IAcademicContract, AcademicContract>();
        services.AddScoped<ISubjectRepository, SubjectRepository>();
        services.AddScoped<ISubjectStageRepository, SubjectStageRepository>();
        services.AddScoped<IAttendanceRecordRepository, AttendanceRecordRepository>();
        services.AddScoped<IAcademicParticipationRepository, AcademicParticipationRepository>();
        services.AddScoped<IAssignmentRepository, AssignmentRepository>();

        RegisterDiscoveredHandlersAndValidators(services);

        return services;
    }

    private static void RegisterDiscoveredHandlersAndValidators(IServiceCollection services)
    {
        var assembly = typeof(AcdAssemblyMarker).Assembly;

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
