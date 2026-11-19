using ElsasaSis.Modules.Tch.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Events;

namespace ElsasaSis.Modules.Tch.Features.Teachers.DeactivateTeacher;

public sealed class DeactivateTeacherHandler(
    ITeacherRepository repository,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<Result<TeacherDto>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var teacher = await repository.GetByIdAsync(id, cancellationToken);
        if (teacher is null)
        {
            return Result<TeacherDto>.Failure(TchErrors.TeacherNotFound(id));
        }

        // BR-TCH-007: Preserves existing assignments when setting teacher status to INACTIVE.
        var deactivateResult = teacher.Deactivate();
        if (deactivateResult.IsFailure)
        {
            return Result<TeacherDto>.Failure(deactivateResult.Error);
        }

        var saveResult = await repository.UpdateAsync(teacher, cancellationToken);
        if (saveResult.IsFailure)
        {
            return Result<TeacherDto>.Failure(saveResult.Error);
        }

        if (teacher.DomainEvents.Count > 0)
        {
            await domainEventDispatcher.DispatchAsync(teacher.DomainEvents, cancellationToken);
            teacher.ClearDomainEvents();
        }

        var activeAssignmentCount = await repository.CountActiveAssignmentsAsync(id, cancellationToken);
        return Result<TeacherDto>.Success(teacher.ToDto(activeAssignmentCount));
    }
}
