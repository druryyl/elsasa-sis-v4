using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Opr.Features.CalendarEntries.DeleteCalendarEntry;

public sealed class DeleteCalendarEntryHandler(ICalendarEntryRepository repository)
{
    public async Task<Result> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entry = await repository.GetByIdAsync(id, cancellationToken);
        if (entry is null)
        {
            return Result.Failure(OprErrors.CalendarEntryNotFound(id));
        }

        await repository.DeleteAsync(id, cancellationToken);
        return Result.Success();
    }
}
