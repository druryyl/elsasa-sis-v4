using ElsasaSis.Modules.Std.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Events;

namespace ElsasaSis.Modules.Std.Features.Students.UpdateStudentGuardian;

public sealed class UpdateStudentGuardianHandler(
    IStudentRepository studentRepository,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<Result<StudentGuardianDto>> HandleAsync(
        Guid id,
        Guid guardianId,
        UpdateStudentGuardianRequest request,
        CancellationToken cancellationToken = default)
    {
        var student = await studentRepository.GetByIdAsync(id, cancellationToken);
        if (student is null)
        {
            return Result<StudentGuardianDto>.Failure(StdErrors.StudentNotFound(id));
        }

        var relTypeResult = StdDomainParsing.ParseRelationshipType(request.RelationshipType);
        if (relTypeResult.IsFailure)
        {
            return Result<StudentGuardianDto>.Failure(relTypeResult.Error);
        }

        var updateResult = student.UpdateGuardian(
            guardianId,
            request.FullName,
            relTypeResult.Value,
            request.Phone,
            request.Email,
            request.Address);

        if (updateResult.IsFailure)
        {
            return Result<StudentGuardianDto>.Failure(updateResult.Error);
        }

        var saveResult = await studentRepository.UpdateAsync(student, cancellationToken);
        if (saveResult.IsFailure)
        {
            return Result<StudentGuardianDto>.Failure(saveResult.Error);
        }

        if (student.DomainEvents.Count > 0)
        {
            await domainEventDispatcher.DispatchAsync(student.DomainEvents, cancellationToken);
            student.ClearDomainEvents();
        }

        return Result<StudentGuardianDto>.Success(updateResult.Value!.ToDto());
    }
}
