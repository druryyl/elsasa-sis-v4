using ElsasaSis.Modules.Tch.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Events;

namespace ElsasaSis.Modules.Tch.Features.Assignments.RemoveTeacherAssignment;

public sealed class RemoveTeacherAssignmentHandler(
    ITeacherAssignmentRepository repository,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<Result> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var assignment = await repository.GetByIdAsync(id, cancellationToken);
        if (assignment is null)
        {
            return Result.Failure(TchErrors.AssignmentNotFound(id));
        }

        var removeResult = assignment.Remove();
        if (removeResult.IsFailure)
        {
            return Result.Failure(removeResult.Error);
        }

        var saveResult = await repository.UpdateAsync(assignment, cancellationToken);
        if (saveResult.IsFailure)
        {
            return Result.Failure(saveResult.Error);
        }

        if (assignment.DomainEvents.Count > 0)
        {
            await domainEventDispatcher.DispatchAsync(assignment.DomainEvents, cancellationToken);
            assignment.ClearDomainEvents();
        }

        return Result.Success();
    }
}
