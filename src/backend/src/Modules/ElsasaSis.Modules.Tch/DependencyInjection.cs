using ElsasaSis.Contracts.Teaching;
using ElsasaSis.Modules.Tch.Domain;
using ElsasaSis.Modules.Tch.Infrastructure.Contracts;
using ElsasaSis.Modules.Tch.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ElsasaSis.Modules.Tch;

public static class DependencyInjection
{
    public static IServiceCollection AddTchModule(this IServiceCollection services)
    {
        services.AddScoped<ITeacherRepository, TeacherRepository>();
        services.AddScoped<ITeacherAssignmentRepository, TeacherAssignmentRepository>();
        services.AddScoped<ITeachingContract, TeachingContract>();

        var assembly = typeof(TchAssemblyMarker).Assembly;

        foreach (var type in assembly.GetTypes())
        {
            if (type is not { IsClass: true, IsAbstract: false, ContainsGenericParameters: false })
            {
                continue;
            }

            if (type.Name.EndsWith("Handler", StringComparison.Ordinal))
            {
                services.AddScoped(type);
            }

            foreach (var validatorInterface in type.GetInterfaces()
                         .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>)))
            {
                services.AddScoped(validatorInterface, type);
            }
        }

        return services;
    }
}
