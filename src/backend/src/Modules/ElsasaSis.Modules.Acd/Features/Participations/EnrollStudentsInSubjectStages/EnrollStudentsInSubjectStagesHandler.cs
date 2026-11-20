using ElsasaSis.Contracts.Operational;
using ElsasaSis.Contracts.Student;
using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.Modules.Acd.Domain.Model;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Acd.Features.Participations.EnrollStudentsInSubjectStages;

public sealed class EnrollStudentsInSubjectStagesHandler(
    IAcademicParticipationRepository participationRepository,
    ISubjectStageRepository subjectStageRepository,
    IOperationalContract operationalContract,
    IStudentContract studentContract)
{
    public async Task<Result<EnrollStudentsInSubjectStagesResponse>> HandleAsync(
        EnrollStudentsInSubjectStagesRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate operationalPeriodId is active via IOperationalContract
        var period = await operationalContract.GetOperationalPeriodByIdAsync(request.OperationalPeriodId, cancellationToken);
        if (period is null)
        {
            return Result<EnrollStudentsInSubjectStagesResponse>.Failure(
                ParticipationErrors.OperationalPeriodNotFound(request.OperationalPeriodId));
        }

        if (!string.Equals(period.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            return Result<EnrollStudentsInSubjectStagesResponse>.Failure(
                ParticipationErrors.OperationalPeriodNotActive(request.OperationalPeriodId));
        }

        // 2. Validate subjectStageId exists via ISubjectStageRepository and retrieve subject stage metadata
        var stage = await subjectStageRepository.GetByIdAsync(request.SubjectStageId, cancellationToken);
        if (stage is null)
        {
            return Result<EnrollStudentsInSubjectStagesResponse>.Failure(
                ParticipationErrors.SubjectStageNotFound(request.SubjectStageId));
        }

        // Validate study group exists via IStudentContract
        var studyGroup = await studentContract.GetStudyGroupByIdAsync(request.StudyGroupId, cancellationToken);
        if (studyGroup is null)
        {
            return Result<EnrollStudentsInSubjectStagesResponse>.Failure(
                ParticipationErrors.StudyGroupNotFound(request.StudyGroupId));
        }

        // 3. Cross-schema query for active students placed in studyGroupId for that academic year
        var placedStudents = await participationRepository.GetActivePlacementsByStudyGroupAsync(
            request.StudyGroupId,
            period.AcademicYearId,
            cancellationToken);

        if (placedStudents.Count == 0)
        {
            return Result<EnrollStudentsInSubjectStagesResponse>.Success(
                new EnrollStudentsInSubjectStagesResponse(0, 0));
        }

        // 4. Query already enrolled student IDs to silently skip
        var studentIds = placedStudents.Select(s => s.StudentId).ToList();
        var alreadyEnrolledIds = await participationRepository.GetEnrolledStudentIdsAsync(
            stage.Id,
            period.Id,
            studentIds,
            cancellationToken);

        var toEnroll = placedStudents.Where(s => !alreadyEnrolledIds.Contains(s.StudentId)).ToList();
        var skippedCount = placedStudents.Count - toEnroll.Count;

        if (toEnroll.Count == 0)
        {
            return Result<EnrollStudentsInSubjectStagesResponse>.Success(
                new EnrollStudentsInSubjectStagesResponse(0, skippedCount));
        }

        // 5. Construct domain entities with display snapshots
        var now = DateTime.UtcNow;
        var entitiesToInsert = new List<AcademicParticipation>();
        foreach (var student in toEnroll)
        {
            var createResult = AcademicParticipation.Create(
                studentId: student.StudentId,
                studentName: student.StudentName,
                studentNis: student.StudentNis,
                subjectStageId: stage.Id,
                subjectStageName: stage.Name,
                subjectId: stage.SubjectId,
                subjectName: stage.SubjectName,
                operationalPeriodId: period.Id,
                operationalPeriodName: period.Name,
                academicYearId: period.AcademicYearId,
                academicYearName: period.AcademicYearName,
                studyGroupId: student.StudyGroupId,
                studyGroupName: student.StudyGroupName,
                status: ParticipationStatus.Active,
                utcNow: now);

            if (!createResult.IsSuccess)
            {
                return Result<EnrollStudentsInSubjectStagesResponse>.Failure(createResult.Error);
            }

            entitiesToInsert.Add(createResult.Value!);
        }

        // 6. Bulk insert into acd.academic_participations with display snapshots
        var actuallyEnrolled = await participationRepository.AddBatchAsync(entitiesToInsert, cancellationToken);
        skippedCount = placedStudents.Count - actuallyEnrolled;

        return Result<EnrollStudentsInSubjectStagesResponse>.Success(
            new EnrollStudentsInSubjectStagesResponse(actuallyEnrolled, skippedCount));
    }
}
