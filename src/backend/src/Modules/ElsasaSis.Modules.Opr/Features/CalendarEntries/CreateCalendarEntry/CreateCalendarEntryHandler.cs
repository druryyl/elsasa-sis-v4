using ElsasaSis.Contracts.Operational;
using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Events;

namespace ElsasaSis.Modules.Opr.Features.CalendarEntries.CreateCalendarEntry;

public sealed class CreateCalendarEntryHandler(
    ICalendarEntryRepository repository,
    IOperationalContract operationalContract,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<Result<CalendarEntryDto>> HandleAsync(
        CreateCalendarEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var academicYear = await operationalContract.GetAcademicYearByIdAsync(request.AcademicYearId, cancellationToken);
        if (academicYear is null)
        {
            return Result<CalendarEntryDto>.Failure(OprErrors.NotFound(request.AcademicYearId));
        }

        var dayTypeResult = CalendarDayTypeExtensions.Parse(request.DayType);
        if (dayTypeResult.IsFailure)
        {
            return Result<CalendarEntryDto>.Failure(dayTypeResult.Error);
        }

        if (await repository.ExistsByAcademicYearAndDateAsync(
                request.AcademicYearId,
                request.EntryDate,
                excludeId: null,
                cancellationToken))
        {
            return Result<CalendarEntryDto>.Failure(
                OprErrors.DuplicateCalendarEntryDate(request.AcademicYearId, request.EntryDate));
        }

        var createResult = CalendarEntry.Create(
            academicYear.Id,
            academicYear.StartDate,
            academicYear.EndDate,
            request.EntryDate,
            dayTypeResult.Value,
            request.Description);

        if (createResult.IsFailure)
        {
            return Result<CalendarEntryDto>.Failure(createResult.Error);
        }

        var entry = createResult.Value!;

        var saveResult = await repository.AddAsync(entry, cancellationToken);
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
