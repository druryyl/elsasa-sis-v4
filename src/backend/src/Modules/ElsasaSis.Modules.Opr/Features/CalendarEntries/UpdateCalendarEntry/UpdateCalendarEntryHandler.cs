using ElsasaSis.Contracts.Operational;
using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Events;

namespace ElsasaSis.Modules.Opr.Features.CalendarEntries.UpdateCalendarEntry;

public sealed class UpdateCalendarEntryHandler(
    ICalendarEntryRepository repository,
    IOperationalContract operationalContract,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<Result<CalendarEntryDto>> HandleAsync(
        Guid id,
        UpdateCalendarEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var entry = await repository.GetByIdAsync(id, cancellationToken);
        if (entry is null)
        {
            return Result<CalendarEntryDto>.Failure(OprErrors.CalendarEntryNotFound(id));
        }

        var academicYear = await operationalContract.GetAcademicYearByIdAsync(entry.AcademicYearId, cancellationToken);
        if (academicYear is null)
        {
            return Result<CalendarEntryDto>.Failure(OprErrors.NotFound(entry.AcademicYearId));
        }

        var dayTypeResult = CalendarDayTypeExtensions.Parse(request.DayType);
        if (dayTypeResult.IsFailure)
        {
            return Result<CalendarEntryDto>.Failure(dayTypeResult.Error);
        }

        if (await repository.ExistsByAcademicYearAndDateAsync(
                entry.AcademicYearId,
                request.EntryDate,
                excludeId: id,
                cancellationToken))
        {
            return Result<CalendarEntryDto>.Failure(
                OprErrors.DuplicateCalendarEntryDate(entry.AcademicYearId, request.EntryDate));
        }

        var updateResult = entry.Update(
            academicYear.StartDate,
            academicYear.EndDate,
            request.EntryDate,
            dayTypeResult.Value,
            request.Description);

        if (updateResult.IsFailure)
        {
            return Result<CalendarEntryDto>.Failure(updateResult.Error);
        }

        var saveResult = await repository.UpdateAsync(entry, cancellationToken);
        if (saveResult.IsFailure)
        {
            return Result<CalendarEntryDto>.Failure(saveResult.Error);
        }

        if (entry.DomainEvents.Count > 0)
        {
            await domainEventDispatcher.DispatchAsync(entry.DomainEvents, cancellationToken);
            entry.ClearDomainEvents();
        }

        return Result<CalendarEntryDto>.Success(entry.ToDto(academicYear.Name));
    }
}
