using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Events;

namespace ElsasaSis.Modules.Opr.Features.AcademicYears.UpdateAcademicYear;

public sealed class UpdateAcademicYearHandler(
    IAcademicYearRepository repository,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<Result<AcademicYearDto>> HandleAsync(
        Guid id,
        UpdateAcademicYearRequest request,
        CancellationToken cancellationToken = default)
    {
        var academicYear = await repository.GetByIdAsync(id, cancellationToken);
        if (academicYear is null)
        {
            return Result<AcademicYearDto>.Failure(OprErrors.NotFound(id));
        }

        var trimmedName = request.Name.Trim();
        if (await repository.ExistsByNameAsync(trimmedName, excludeId: id, cancellationToken))
        {
            return Result<AcademicYearDto>.Failure(OprErrors.DuplicateYearName(trimmedName));
        }

        var periodInputs = request.Periods
            .Select(p => new OperationalPeriodInput(p.Id, p.Name, p.StartDate, p.EndDate))
            .ToArray();

        var updateResult = academicYear.Update(
            request.Name,
            request.StartDate,
            request.EndDate,
            periodInputs);

        if (updateResult.IsFailure)
        {
            return Result<AcademicYearDto>.Failure(updateResult.Error);
        }

        await repository.UpdateAsync(academicYear, cancellationToken);

        if (academicYear.DomainEvents.Count > 0)
        {
            await domainEventDispatcher.DispatchAsync(academicYear.DomainEvents, cancellationToken);
            academicYear.ClearDomainEvents();
        }

        return Result<AcademicYearDto>.Success(academicYear.ToDto());
    }
}
