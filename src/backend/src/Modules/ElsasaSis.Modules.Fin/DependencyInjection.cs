using ElsasaSis.Contracts.Finance;
using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.Modules.Fin.Infrastructure.Contracts;
using ElsasaSis.Modules.Fin.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ElsasaSis.Modules.Fin;

public static class DependencyInjection
{
    public static IServiceCollection AddFinModule(this IServiceCollection services)
    {
        services.AddScoped<IFinanceModule, FinanceModuleContract>();
        services.AddScoped<IChargeTypeRepository, ChargeTypeRepository>();
        services.AddScoped<IStudentDiscountRepository, StudentDiscountRepository>();
        services.AddScoped<IStudentChargeRepository, StudentChargeRepository>();
        services.AddScoped<IPaymentReceiptRepository, PaymentReceiptRepository>();
        services.AddScoped<IReceiptSequenceGenerator, ReceiptSequenceGenerator>();

        RegisterDiscoveredHandlersAndValidators(services);

        return services;
    }

    private static void RegisterDiscoveredHandlersAndValidators(IServiceCollection services)
    {
        var assembly = typeof(FinAssemblyMarker).Assembly;

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
