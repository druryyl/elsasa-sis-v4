using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Events;

namespace ElsasaSis.Modules.Opr.Features.AcademicYears.CreateAcademicYear;

public sealed class CreateAcademicYearHandler(
    IAcademicYearRepository repository,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<Result<AcademicYearDto>> HandleAsync(
        CreateAcademicYearRequest request,
        CancellationToken cancellationToken = default)
    {
        var trimmedName = request.Name.Trim();
        if (await repository.ExistsByNameAsync(trimmedName, excludeId: null, cancellationToken))
        {
            return Result<AcademicYearDto>.Failure(OprErrors.DuplicateYearName(trimmedName));
        }

        var periodInputs = request.Periods
            .Select(p => new OperationalPeriodInput(null, p.Name, p.StartDate, p.EndDate))
            .ToArray();

        var createResult = AcademicYear.Create(
            request.Name,
            request.StartDate,
            request.EndDate,
            periodInputs);

        if (createResult.IsFailure)
        {
            return Result<AcademicYearDto>.Failure(createResult.Error);
        }

        var academicYear = createResult.Value!;

        await repository.AddAsync(academicYear, cancellationToken);

        if (academicYear.DomainEvents.Count > 0)
        {
            await domainEventDispatcher.DispatchAsync(academicYear.DomainEvents, cancellationToken);
            academicYear.ClearDomainEvents();
        }

        return Result<AcademicYearDto>.Success(academicYear.ToDto());
    }
}
