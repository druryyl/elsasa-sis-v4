using ElsasaSis.Modules.Tch.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Events;

namespace ElsasaSis.Modules.Tch.Features.Teachers.CreateTeacher;

public sealed class CreateTeacherHandler(
    ITeacherRepository repository,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<Result<TeacherDto>> HandleAsync(
        CreateTeacherRequest request,
        CancellationToken cancellationToken = default)
    {
        var genderResult = Teacher.ParseGender(request.Gender);
        if (genderResult.IsFailure)
        {
            return Result<TeacherDto>.Failure(genderResult.Error);
        }

        var registerResult = Teacher.Register(
            request.FullName,
            request.DateOfBirth,
            genderResult.Value,
            request.Nickname,
            request.Nip,
            request.Nik,
            request.Phone,
            request.Email,
            request.Address);

        if (registerResult.IsFailure)
        {
            return Result<TeacherDto>.Failure(registerResult.Error);
        }

        var teacher = registerResult.Value!;

        if (teacher.Nip is not null &&
            await repository.ExistsByNipAsync(teacher.Nip, excludeId: null, cancellationToken))
        {
            return Result<TeacherDto>.Failure(TchErrors.DuplicateNip(teacher.Nip));
        }

        if (teacher.Nik is not null &&
            await repository.ExistsByNikAsync(teacher.Nik, excludeId: null, cancellationToken))
        {
            return Result<TeacherDto>.Failure(TchErrors.DuplicateNik(teacher.Nik));
        }

        var saveResult = await repository.AddAsync(teacher, cancellationToken);
        if (saveResult.IsFailure)
        {
            return Result<TeacherDto>.Failure(saveResult.Error);
        }

        if (teacher.DomainEvents.Count > 0)
        {
            await domainEventDispatcher.DispatchAsync(teacher.DomainEvents, cancellationToken);
            teacher.ClearDomainEvents();
        }

        return Result<TeacherDto>.Success(teacher.ToDto(activeAssignmentCount: 0));
    }
}
