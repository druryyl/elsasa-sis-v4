using ElsasaSis.Contracts.Operational;
using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.Modules.Opr.Features.AcademicYears.ActivateAcademicYear;
using ElsasaSis.Modules.Opr.Features.AcademicYears.ActivateOperationalPeriod;
using ElsasaSis.Modules.Opr.Features.AcademicYears.CreateAcademicYear;
using ElsasaSis.Modules.Opr.Features.AcademicYears.GetAcademicYearById;
using ElsasaSis.Modules.Opr.Features.AcademicYears.GetAcademicYears;
using ElsasaSis.Modules.Opr.Features.AcademicYears.UpdateAcademicYear;
using ElsasaSis.Modules.Opr.Infrastructure.Contracts;
using ElsasaSis.Modules.Opr.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ElsasaSis.Modules.Opr;

public static class DependencyInjection
{
    public static IServiceCollection AddOprModule(this IServiceCollection services)
    {
        services.AddScoped<IAcademicYearRepository, AcademicYearRepository>();
        services.AddScoped<IOperationalContract, OperationalContract>();

        services.AddScoped<GetAcademicYearsHandler>();
        services.AddScoped<GetAcademicYearByIdHandler>();
        services.AddScoped<CreateAcademicYearHandler>();
        services.AddScoped<UpdateAcademicYearHandler>();
        services.AddScoped<ActivateAcademicYearHandler>();
        services.AddScoped<ActivateOperationalPeriodHandler>();

        services.AddScoped<IValidator<CreateAcademicYearRequest>, CreateAcademicYearValidator>();
        services.AddScoped<IValidator<UpdateAcademicYearRequest>, UpdateAcademicYearValidator>();

        return services;
    }
}
