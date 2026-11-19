using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Events;

namespace ElsasaSis.Modules.Opr.Features.AcademicYears.ActivateAcademicYear;

public sealed class ActivateAcademicYearHandler(
    IAcademicYearRepository repository,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<Result<AcademicYearDto>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var academicYear = await repository.GetByIdAsync(id, cancellationToken);
        if (academicYear is null)
        {
            return Result<AcademicYearDto>.Failure(OprErrors.NotFound(id));
        }

        var activateResult = academicYear.Activate();
        if (activateResult.IsFailure)
        {
            return Result<AcademicYearDto>.Failure(activateResult.Error);
        }

        await repository.SaveActivationAsync(academicYear, cancellationToken);

        if (academicYear.DomainEvents.Count > 0)
        {
            await domainEventDispatcher.DispatchAsync(academicYear.DomainEvents, cancellationToken);
            academicYear.ClearDomainEvents();
        }

        return Result<AcademicYearDto>.Success(academicYear.ToDto());
    }
}
