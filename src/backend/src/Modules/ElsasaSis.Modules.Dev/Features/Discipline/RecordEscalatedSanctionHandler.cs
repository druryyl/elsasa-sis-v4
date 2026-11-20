using ElsasaSis.Modules.Dev.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Events;
using ElsasaSis.SharedKernel.Security;

namespace ElsasaSis.Modules.Dev.Features.Discipline;

public sealed class RecordEscalatedSanctionHandler(
    IDisciplineRecordRepository repository,
    ICurrentUser currentUser,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<Result<DisciplineRecordDto>> HandleAsync(
        RecordEscalatedSanctionRequest request,
        CancellationToken cancellationToken = default)
    {
        var recorderId = currentUser.UserId != Guid.Empty
            ? currentUser.UserId
            : (request.RecorderId ?? Guid.Empty);

        var recorderName = !string.IsNullOrWhiteSpace(currentUser.FullName)
            ? currentUser.FullName
            : (!string.IsNullOrWhiteSpace(currentUser.Username)
                ? currentUser.Username
                : (!string.IsNullOrWhiteSpace(request.RecorderName) ? request.RecorderName : string.Empty));

        var recordResult = DisciplineRecord.CreateIncident(
            request.StudentId,
            request.StudentName,
            request.StudentNis,
            request.AcademicYearId,
            request.AcademicYearName,
            request.IncidentDate,
            request.ViolationType,
            request.Description,
            request.Sanction,
            recorderId,
            recorderName,
            role: "PRINCIPAL");

        if (recordResult.IsFailure)
        {
            return Result<DisciplineRecordDto>.Failure(recordResult.Error);
        }

        var record = recordResult.Value!;
        await repository.InsertAsync(record, cancellationToken);

        if (record.DomainEvents.Count > 0)
        {
            await domainEventDispatcher.DispatchAsync(record.DomainEvents, cancellationToken);
            record.ClearDomainEvents();
        }

        return Result<DisciplineRecordDto>.Success(new DisciplineRecordDto(
            record.Id,
            record.StudentId,
            record.StudentName,
            record.StudentNis,
            record.AcademicYearId,
            record.AcademicYearName,
            record.IncidentDate,
            record.ViolationType,
            record.Description,
            record.Sanction,
            record.RecorderId,
            record.RecorderName,
            record.CreatedAt));
    }
}
