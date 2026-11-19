using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Opr.Domain;

public interface ICalendarEntryRepository
{
    Task<CalendarEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByAcademicYearAndDateAsync(Guid academicYearId, DateOnly entryDate, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<Result> AddAsync(CalendarEntry entry, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(CalendarEntry entry, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
