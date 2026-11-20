using ElsasaSis.SharedKernel.Common;
using MediatR;

namespace ElsasaSis.Modules.Acd.Features.Monitoring.GetAcademicMonitoringProgress;

public sealed record GetAcademicMonitoringProgressQuery(
    Guid? AcademicYearId = null,
    Guid? OperationalPeriodId = null,
    DateOnly? Date = null) : IRequest<Result<AcademicMonitoringProgressDto>>;
