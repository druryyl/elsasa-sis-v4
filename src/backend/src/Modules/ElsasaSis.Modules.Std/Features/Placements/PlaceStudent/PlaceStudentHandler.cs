using ElsasaSis.Contracts.Operational;
using ElsasaSis.Modules.Std.Domain;
using ElsasaSis.Modules.Std.Features.Students;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Events;

namespace ElsasaSis.Modules.Std.Features.Placements.PlaceStudent;

public sealed class PlaceStudentHandler(
    IStudentRepository studentRepository,
    IStudyGroupRepository studyGroupRepository,
    IStudentPlacementRepository placementRepository,
    IOperationalContract operationalContract,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<Result<StudentPlacementDto>> HandleAsync(
        Guid studentId,
        PlaceStudentRequest request,
        CancellationToken cancellationToken = default)
    {
        var student = await studentRepository.GetByIdAsync(studentId, cancellationToken);
        if (student is null)
        {
            return Result<StudentPlacementDto>.Failure(StdErrors.StudentNotFound(studentId));
        }

        var academicYear = await operationalContract.GetAcademicYearByIdAsync(request.AcademicYearId, cancellationToken);
        if (academicYear is null)
        {
            return Result<StudentPlacementDto>.Failure(StdErrors.AcademicYearNotFound(request.AcademicYearId));
        }

        var schoolLevelResult = StdDomainParsing.ParseSchoolLevel(request.SchoolLevel);
        if (schoolLevelResult.IsFailure)
        {
            return Result<StudentPlacementDto>.Failure(schoolLevelResult.Error);
        }

        var schoolLevel = schoolLevelResult.Value;

        StudyGroup? studyGroup = null;
        if (request.StudyGroupId.HasValue)
        {
            studyGroup = await studyGroupRepository.GetByIdAsync(request.StudyGroupId.Value, cancellationToken);
            if (studyGroup is null)
            {
                return Result<StudentPlacementDto>.Failure(StdErrors.StudyGroupNotFound(request.StudyGroupId.Value));
            }

            var trimmedGrade = string.IsNullOrWhiteSpace(request.Grade) ? string.Empty : request.Grade.Trim();
            if (studyGroup.AcademicYearId != request.AcademicYearId ||
                studyGroup.SchoolLevel != schoolLevel ||
                !string.Equals(studyGroup.Grade, trimmedGrade, StringComparison.OrdinalIgnoreCase))
            {
                return Result<StudentPlacementDto>.Failure(StdErrors.PlacementStudyGroupMismatch(studyGroup.Id));
            }
        }

        var existingPlacement = await placementRepository.GetByStudentAndAcademicYearAsync(
            studentId,
            request.AcademicYearId,
            cancellationToken);

        var placementResult = StudentPlacement.CreateOrUpdateForStudyGroup(
            existingPlacement,
            studentId,
            request.AcademicYearId,
            academicYear.Name,
            schoolLevel,
            request.Grade,
            studyGroup);

        if (placementResult.IsFailure)
        {
            return Result<StudentPlacementDto>.Failure(placementResult.Error);
        }

        var placement = placementResult.Value!;
        var saveResult = await placementRepository.UpsertAsync(placement, cancellationToken);
        if (saveResult.IsFailure)
        {
            return Result<StudentPlacementDto>.Failure(saveResult.Error);
        }

        if (placement.DomainEvents.Count > 0)
        {
            await domainEventDispatcher.DispatchAsync(placement.DomainEvents, cancellationToken);
            placement.ClearDomainEvents();
        }

        return Result<StudentPlacementDto>.Success(placement.ToDto(
            studyGroup?.HomeroomTeacherId,
            studyGroup?.HomeroomTeacherName));
    }
}
