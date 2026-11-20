using ElsasaSis.Modules.Dev.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Events;
using ElsasaSis.SharedKernel.Security;

namespace ElsasaSis.Modules.Dev.Features.Behaviour;

public sealed class RecordBehaviourNoteHandler(
    IBehaviourNoteRepository repository,
    ICurrentUser currentUser,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<Result<BehaviourNoteDto>> HandleAsync(
        RecordBehaviourNoteRequest request,
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

        var noteResult = BehaviourNote.Create(
            request.StudentId,
            request.StudentName,
            request.StudentNis,
            request.AcademicYearId,
            request.AcademicYearName,
            request.BehaviourType,
            request.ObservedDate,
            request.Description,
            recorderId,
            recorderName);

        if (noteResult.IsFailure)
        {
            return Result<BehaviourNoteDto>.Failure(noteResult.Error);
        }

        var note = noteResult.Value!;
        await repository.InsertAsync(note, cancellationToken);

        if (note.DomainEvents.Count > 0)
        {
            await domainEventDispatcher.DispatchAsync(note.DomainEvents, cancellationToken);
            note.ClearDomainEvents();
        }

        return Result<BehaviourNoteDto>.Success(new BehaviourNoteDto(
            note.Id,
            note.StudentId,
            note.StudentName,
            note.StudentNis,
            note.AcademicYearId,
            note.AcademicYearName,
            note.BehaviourType,
            note.ObservedDate,
            note.Description,
            note.RecorderId,
            note.RecorderName,
            note.CreatedAt));
    }
}
