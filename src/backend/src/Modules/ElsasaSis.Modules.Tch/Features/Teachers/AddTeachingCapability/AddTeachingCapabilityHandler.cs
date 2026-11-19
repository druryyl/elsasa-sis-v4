using ElsasaSis.Contracts.Academic;
using ElsasaSis.Modules.Tch.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Events;

namespace ElsasaSis.Modules.Tch.Features.Teachers.AddTeachingCapability;

public sealed class AddTeachingCapabilityHandler(
    ITeacherRepository repository,
    IAcademicContract academicContract,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<Result<TeachingCapabilityDto>> HandleAsync(
        Guid teacherId,
        AddTeachingCapabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        var teacher = await repository.GetByIdAsync(teacherId, cancellationToken);
        if (teacher is null)
        {
            return Result<TeachingCapabilityDto>.Failure(TchErrors.TeacherNotFound(teacherId));
        }

        var subject = await academicContract.GetSubjectByIdAsync(request.SubjectId, cancellationToken);
        if (subject is null)
        {
            return Result<TeachingCapabilityDto>.Failure(TchErrors.SubjectNotFound(request.SubjectId));
        }

        var addResult = teacher.AddCapability(subject.Id, subject.Name);
        if (addResult.IsFailure)
        {
            return Result<TeachingCapabilityDto>.Failure(addResult.Error);
        }

        var saveResult = await repository.UpdateAsync(teacher, cancellationToken);
        if (saveResult.IsFailure)
        {
            return Result<TeachingCapabilityDto>.Failure(saveResult.Error);
        }

        if (teacher.DomainEvents.Count > 0)
        {
            await domainEventDispatcher.DispatchAsync(teacher.DomainEvents, cancellationToken);
            teacher.ClearDomainEvents();
        }

        return Result<TeachingCapabilityDto>.Success(addResult.Value!.ToDto());
    }
}
