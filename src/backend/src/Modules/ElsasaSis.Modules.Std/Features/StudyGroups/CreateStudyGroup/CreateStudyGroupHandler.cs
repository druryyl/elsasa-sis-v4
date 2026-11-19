using ElsasaSis.Contracts.Operational;
using ElsasaSis.Contracts.Teaching;
using ElsasaSis.Modules.Std.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Events;

namespace ElsasaSis.Modules.Std.Features.StudyGroups.CreateStudyGroup;

public sealed class CreateStudyGroupHandler(
    IStudyGroupRepository studyGroupRepository,
    IOperationalContract operationalContract,
    ITeachingContract teachingContract,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<Result<StudyGroupDto>> HandleAsync(
        CreateStudyGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        var academicYear = await operationalContract.GetAcademicYearByIdAsync(request.AcademicYearId, cancellationToken);
        if (academicYear is null)
        {
            return Result<StudyGroupDto>.Failure(StdErrors.AcademicYearNotFound(request.AcademicYearId));
        }

        string? homeroomTeacherName = null;
        if (request.HomeroomTeacherId.HasValue)
        {
            var teacher = await teachingContract.GetTeacherByIdAsync(request.HomeroomTeacherId.Value, cancellationToken);
            if (teacher is null)
            {
                return Result<StudyGroupDto>.Failure(StdErrors.HomeroomTeacherNotFound(request.HomeroomTeacherId.Value));
            }

            if (!string.Equals(teacher.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                return Result<StudyGroupDto>.Failure(StdErrors.HomeroomTeacherInactive(request.HomeroomTeacherId.Value));
            }

            homeroomTeacherName = teacher.FullName;
        }

        var schoolLevelResult = StdDomainParsing.ParseSchoolLevel(request.SchoolLevel);
        if (schoolLevelResult.IsFailure)
        {
            return Result<StudyGroupDto>.Failure(schoolLevelResult.Error);
        }

        var duplicateName = await studyGroupRepository.ExistsByYearAndNameAsync(
            request.AcademicYearId,
            request.Name,
            cancellationToken: cancellationToken);

        if (duplicateName)
        {
            return Result<StudyGroupDto>.Failure(StdErrors.DuplicateStudyGroupName(request.AcademicYearId, request.Name));
        }

        var createResult = StudyGroup.Create(
            request.AcademicYearId,
            academicYear.Name,
            schoolLevelResult.Value,
            request.Grade,
            request.Name,
            request.HomeroomTeacherId,
            homeroomTeacherName);

        if (createResult.IsFailure)
        {
            return Result<StudyGroupDto>.Failure(createResult.Error);
        }

        var studyGroup = createResult.Value!;
        var saveResult = await studyGroupRepository.AddAsync(studyGroup, cancellationToken);
        if (saveResult.IsFailure)
        {
            return Result<StudyGroupDto>.Failure(saveResult.Error);
        }

        if (studyGroup.DomainEvents.Count > 0)
        {
            await domainEventDispatcher.DispatchAsync(studyGroup.DomainEvents, cancellationToken);
            studyGroup.ClearDomainEvents();
        }

        return Result<StudyGroupDto>.Success(studyGroup.ToDto());
    }
}
