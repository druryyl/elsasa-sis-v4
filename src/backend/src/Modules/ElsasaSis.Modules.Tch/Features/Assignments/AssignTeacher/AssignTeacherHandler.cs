using ElsasaSis.Contracts.Academic;
using ElsasaSis.Contracts.Operational;
using ElsasaSis.Contracts.Student;
using ElsasaSis.Modules.Tch.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Events;

namespace ElsasaSis.Modules.Tch.Features.Assignments.AssignTeacher;

public sealed class AssignTeacherHandler(
    ITeacherRepository teacherRepository,
    ITeacherAssignmentRepository assignmentRepository,
    IOperationalContract operationalContract,
    IAcademicContract academicContract,
    IStudentContract studentContract,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<Result<TeacherAssignmentDto>> HandleAsync(
        AssignTeacherRequest request,
        CancellationToken cancellationToken = default)
    {
        var teacher = await teacherRepository.GetByIdAsync(request.TeacherId, cancellationToken);
        if (teacher is null)
        {
            return Result<TeacherAssignmentDto>.Failure(TchErrors.TeacherNotFound(request.TeacherId));
        }

        var period = await operationalContract.GetOperationalPeriodByIdAsync(request.OperationalPeriodId, cancellationToken);
        if (period is null)
        {
            return Result<TeacherAssignmentDto>.Failure(TchErrors.OperationalPeriodNotFound(request.OperationalPeriodId));
        }

        var studyGroup = await studentContract.GetStudyGroupByIdAsync(request.StudyGroupId, cancellationToken);
        if (studyGroup is null)
        {
            return Result<TeacherAssignmentDto>.Failure(TchErrors.StudyGroupNotFound(request.StudyGroupId));
        }

        if (studyGroup.AcademicYearId != period.AcademicYearId)
        {
            return Result<TeacherAssignmentDto>.Failure(
                TchErrors.StudyGroupAcademicYearMismatch(studyGroup.Id, period.AcademicYearId));
        }

        var subject = await academicContract.GetSubjectByIdAsync(request.SubjectId, cancellationToken);
        if (subject is null)
        {
            return Result<TeacherAssignmentDto>.Failure(TchErrors.SubjectNotFound(request.SubjectId));
        }

        var existing = await assignmentRepository.GetByTupleAsync(
            teacher.Id,
            subject.Id,
            studyGroup.Id,
            period.Id,
            cancellationToken);

        TeacherAssignment assignment;
        if (existing is not null)
        {
            var reactivateResult = existing.Reactivate(
                teacher,
                period.AcademicYearName,
                period.Name,
                subject.Name,
                studyGroup.Name);

            if (reactivateResult.IsFailure)
            {
                return Result<TeacherAssignmentDto>.Failure(reactivateResult.Error);
            }

            var updateResult = await assignmentRepository.UpdateAsync(existing, cancellationToken);
            if (updateResult.IsFailure)
            {
                return Result<TeacherAssignmentDto>.Failure(updateResult.Error);
            }

            assignment = existing;
        }
        else
        {
            var assignResult = TeacherAssignment.Assign(
                teacher,
                period.AcademicYearId,
                period.AcademicYearName,
                period.Id,
                period.Name,
                subject.Id,
                subject.Name,
                studyGroup.Id,
                studyGroup.Name);

            if (assignResult.IsFailure)
            {
                return Result<TeacherAssignmentDto>.Failure(assignResult.Error);
            }

            assignment = assignResult.Value!;

            var addResult = await assignmentRepository.AddAsync(assignment, cancellationToken);
            if (addResult.IsFailure)
            {
                return Result<TeacherAssignmentDto>.Failure(addResult.Error);
            }
        }

        if (assignment.DomainEvents.Count > 0)
        {
            await domainEventDispatcher.DispatchAsync(assignment.DomainEvents, cancellationToken);
            assignment.ClearDomainEvents();
        }

        return Result<TeacherAssignmentDto>.Success(assignment.ToDto());
    }
}
