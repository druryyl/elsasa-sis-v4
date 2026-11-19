using ElsasaSis.Modules.Std.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Events;

namespace ElsasaSis.Modules.Std.Features.Students.UpdateStudent;

public sealed class UpdateStudentHandler(
    IStudentRepository studentRepository,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<Result<StudentDto>> HandleAsync(
        Guid id,
        UpdateStudentRequest request,
        CancellationToken cancellationToken = default)
    {
        var student = await studentRepository.GetByIdAsync(id, cancellationToken);
        if (student is null)
        {
            return Result<StudentDto>.Failure(StdErrors.StudentNotFound(id));
        }

        var genderResult = StdDomainParsing.ParseGender(request.Gender);
        if (genderResult.IsFailure)
        {
            return Result<StudentDto>.Failure(genderResult.Error);
        }

        var normalizedNisn = StdDomainParsing.NormalizeOptional(request.Nisn);
        if (normalizedNisn is not null)
        {
            var nisnExists = await studentRepository.ExistsByNisnAsync(normalizedNisn, excludeStudentId: id, cancellationToken);
            if (nisnExists)
            {
                return Result<StudentDto>.Failure(StdErrors.DuplicateNisn(normalizedNisn));
            }
        }

        var updateResult = student.UpdateProfile(
            normalizedNisn,
            request.FullName,
            request.Nickname,
            request.DateOfBirth,
            genderResult.Value,
            request.Phone,
            request.Email,
            request.Address);

        if (updateResult.IsFailure)
        {
            return Result<StudentDto>.Failure(updateResult.Error);
        }

        var saveResult = await studentRepository.UpdateAsync(student, cancellationToken);
        if (saveResult.IsFailure)
        {
            return Result<StudentDto>.Failure(saveResult.Error);
        }

        if (student.DomainEvents.Count > 0)
        {
            await domainEventDispatcher.DispatchAsync(student.DomainEvents, cancellationToken);
            student.ClearDomainEvents();
        }

        return Result<StudentDto>.Success(student.ToDto());
    }
}
