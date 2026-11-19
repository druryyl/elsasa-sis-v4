using ElsasaSis.Modules.Tch.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Events;

namespace ElsasaSis.Modules.Tch.Features.Teachers.RemoveTeachingCapability;

public sealed class RemoveTeachingCapabilityHandler(
    ITeacherRepository repository,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<Result> HandleAsync(
        Guid teacherId,
        Guid subjectId,
        CancellationToken cancellationToken = default)
    {
        var teacher = await repository.GetByIdAsync(teacherId, cancellationToken);
        if (teacher is null)
        {
            return Result.Failure(TchErrors.TeacherNotFound(teacherId));
        }

        var removeResult = teacher.RemoveCapability(subjectId);
        if (removeResult.IsFailure)
        {
            return Result.Failure(removeResult.Error);
        }

        var saveResult = await repository.UpdateAsync(teacher, cancellationToken);
        if (saveResult.IsFailure)
        {
            return Result.Failure(saveResult.Error);
        }

        if (teacher.DomainEvents.Count > 0)
        {
            await domainEventDispatcher.DispatchAsync(teacher.DomainEvents, cancellationToken);
            teacher.ClearDomainEvents();
        }

        return Result.Success();
    }
}
