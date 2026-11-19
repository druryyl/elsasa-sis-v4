using ElsasaSis.Modules.Std.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Events;

namespace ElsasaSis.Modules.Std.Features.Students.RemoveStudentGuardian;

public sealed class RemoveStudentGuardianHandler(
    IStudentRepository studentRepository,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<Result> HandleAsync(
        Guid id,
        Guid guardianId,
        CancellationToken cancellationToken = default)
    {
        var student = await studentRepository.GetByIdAsync(id, cancellationToken);
        if (student is null)
        {
            return Result.Failure(StdErrors.StudentNotFound(id));
        }

        var removeResult = student.RemoveGuardian(guardianId);
        if (removeResult.IsFailure)
        {
            return removeResult;
        }

        var saveResult = await studentRepository.UpdateAsync(student, cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult;
        }

        if (student.DomainEvents.Count > 0)
        {
            await domainEventDispatcher.DispatchAsync(student.DomainEvents, cancellationToken);
            student.ClearDomainEvents();
        }

        return Result.Success();
    }
}
