using ElsasaSis.Contracts.Operational;
using ElsasaSis.Contracts.Student;
using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.Modules.Acd.Domain.Model;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Security;

namespace ElsasaSis.Modules.Acd.Features.Scorebook.CreateClassroomAssignment;

public sealed class CreateClassroomAssignmentHandler(
    IAssignmentRepository assignmentRepository,
    ISubjectRepository subjectRepository,
    ISubjectStageRepository stageRepository,
    IOperationalContract operationalContract,
    IStudentContract studentContract,
    ICurrentUser currentUser)
{
    public async Task<Result<AssignmentDto>> HandleAsync(
        CreateAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var period = await operationalContract.GetOperationalPeriodByIdAsync(request.OperationalPeriodId, cancellationToken);
        if (period is null)
        {
            return Result<AssignmentDto>.Failure(ScorebookErrors.OperationalPeriodNotFound(request.OperationalPeriodId));
        }

        var subject = await subjectRepository.GetByIdAsync(request.SubjectId, cancellationToken);
        if (subject is null)
        {
            return Result<AssignmentDto>.Failure(ScorebookErrors.SubjectNotFound(request.SubjectId));
        }

        var stage = await stageRepository.GetByIdAsync(request.SubjectStageId, cancellationToken);
        if (stage is null)
        {
            return Result<AssignmentDto>.Failure(ScorebookErrors.SubjectStageNotFound(request.SubjectStageId));
        }

        if (stage.SubjectId != request.SubjectId)
        {
            return Result<AssignmentDto>.Failure(ScorebookErrors.SubjectStageNotFound(request.SubjectStageId));
        }

        string? learningUnitName = null;
        if (request.LearningUnitId.HasValue && request.LearningUnitId.Value != Guid.Empty)
        {
            var unit = stage.LearningUnits.FirstOrDefault(u => u.Id == request.LearningUnitId.Value);
            if (unit is null)
            {
                return Result<AssignmentDto>.Failure(ScorebookErrors.LearningUnitNotFound(request.LearningUnitId.Value));
            }

            learningUnitName = unit.Name;
        }

        var studyGroup = await studentContract.GetStudyGroupByIdAsync(request.StudyGroupId, cancellationToken);
        if (studyGroup is null)
        {
            return Result<AssignmentDto>.Failure(ScorebookErrors.StudyGroupNotFound(request.StudyGroupId));
        }

        // Authorization check (D-04): Validate that the teacher has a valid teaching assignment
        Guid teacherId;
        string teacherName;

        if (currentUser.TeacherId.HasValue && currentUser.TeacherId.Value != Guid.Empty)
        {
            var isAssigned = await assignmentRepository.HasTeacherAssignmentAsync(
                currentUser.TeacherId.Value,
                request.StudyGroupId,
                request.SubjectId,
                request.OperationalPeriodId,
                cancellationToken);

            if (!isAssigned)
            {
                return Result<AssignmentDto>.Failure(ScorebookErrors.TeacherNotAuthorized(
                    currentUser.TeacherId.Value,
                    request.StudyGroupId,
                    request.SubjectId));
            }

            teacherId = currentUser.TeacherId.Value;
            teacherName = !string.IsNullOrWhiteSpace(currentUser.FullName)
                ? currentUser.FullName
                : (!string.IsNullOrWhiteSpace(currentUser.Username) ? currentUser.Username : "Teacher");
        }
        else
        {
            var activeTeacher = await assignmentRepository.GetActiveTeacherAssignmentAsync(
                request.StudyGroupId,
                request.SubjectId,
                request.OperationalPeriodId,
                cancellationToken);

            if (activeTeacher is null)
            {
                return Result<AssignmentDto>.Failure(ScorebookErrors.NoActiveTeacherAssigned(
                    request.StudyGroupId,
                    request.SubjectId));
            }

            teacherId = activeTeacher.TeacherId;
            teacherName = activeTeacher.TeacherName;
        }

        var typeResult = AssignmentTypeExtensions.ParseAssignmentType(request.AssignmentType);
        if (typeResult.IsFailure)
        {
            return Result<AssignmentDto>.Failure(typeResult.Error);
        }

        var createResult = Assignment.Create(
            stage.Id,
            stage.Name,
            subject.Id,
            subject.Name,
            period.Id,
            period.Name,
            studyGroup.Id,
            studyGroup.Name,
            request.Title,
            typeResult.Value,
            request.MaxScore,
            request.Weight,
            request.AssignedDate,
            request.DueDate,
            teacherId,
            teacherName,
            request.LearningUnitId,
            learningUnitName);

        if (createResult.IsFailure)
        {
            return Result<AssignmentDto>.Failure(createResult.Error);
        }

        var assignment = createResult.Value!;
        await assignmentRepository.AddAsync(assignment, cancellationToken);

        return Result<AssignmentDto>.Success(new AssignmentDto(
            assignment.Id,
            assignment.SubjectStageId,
            assignment.SubjectStageName,
            assignment.SubjectId,
            assignment.SubjectName,
            assignment.LearningUnitId,
            assignment.LearningUnitName,
            assignment.OperationalPeriodId,
            assignment.OperationalPeriodName,
            assignment.StudyGroupId!.Value,
            assignment.StudyGroupName!,
            assignment.Title,
            assignment.AssignmentType.ToCode(),
            assignment.MaxScore,
            assignment.Weight,
            assignment.AssignedDate,
            assignment.DueDate,
            assignment.TeacherId,
            assignment.TeacherName,
            assignment.CreatedAt,
            assignment.UpdatedAt));
    }
}
