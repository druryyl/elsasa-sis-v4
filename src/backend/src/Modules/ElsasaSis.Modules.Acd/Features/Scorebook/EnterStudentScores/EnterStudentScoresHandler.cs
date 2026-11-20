using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Security;

namespace ElsasaSis.Modules.Acd.Features.Scorebook.EnterStudentScores;

public sealed class EnterStudentScoresHandler(
    IAssignmentRepository assignmentRepository,
    ICurrentUser currentUser)
{
    public async Task<Result<EnterStudentScoresResponse>> HandleAsync(
        Guid assignmentId,
        EnterStudentScoresRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Items is null || request.Items.Count == 0)
        {
            return Result<EnterStudentScoresResponse>.Failure(ScorebookErrors.ItemsRequired());
        }

        var seenStudentIds = new HashSet<Guid>();
        foreach (var item in request.Items)
        {
            if (!seenStudentIds.Add(item.StudentId))
            {
                return Result<EnterStudentScoresResponse>.Failure(ScorebookErrors.DuplicateStudentInBatch(item.StudentId));
            }
        }

        var assignment = await assignmentRepository.GetByIdAsync(assignmentId, cancellationToken);
        if (assignment is null)
        {
            return Result<EnterStudentScoresResponse>.Failure(ScorebookErrors.AssignmentNotFound(assignmentId));
        }

        if (!assignment.StudyGroupId.HasValue)
        {
            return Result<EnterStudentScoresResponse>.Failure(ScorebookErrors.StudyGroupIdRequired());
        }

        var studyGroupId = assignment.StudyGroupId.Value;

        // Authorization check (D-04): Validate that the teacher has a valid teaching assignment
        if (currentUser.TeacherId.HasValue && currentUser.TeacherId.Value != Guid.Empty)
        {
            var isAssigned = await assignmentRepository.HasTeacherAssignmentAsync(
                currentUser.TeacherId.Value,
                studyGroupId,
                assignment.SubjectId,
                assignment.OperationalPeriodId,
                cancellationToken);

            if (!isAssigned)
            {
                return Result<EnterStudentScoresResponse>.Failure(ScorebookErrors.TeacherNotAuthorized(
                    currentUser.TeacherId.Value,
                    studyGroupId,
                    assignment.SubjectId));
            }
        }
        else
        {
            var activeTeacher = await assignmentRepository.GetActiveTeacherAssignmentAsync(
                studyGroupId,
                assignment.SubjectId,
                assignment.OperationalPeriodId,
                cancellationToken);

            if (activeTeacher is null)
            {
                return Result<EnterStudentScoresResponse>.Failure(ScorebookErrors.NoActiveTeacherAssigned(
                    studyGroupId,
                    assignment.SubjectId));
            }
        }

        // Validate that each student has ACTIVE participation in the assignment's subject_stage_id
        var activeParticipations = await assignmentRepository.GetActiveParticipationsAsync(
            assignment.SubjectStageId,
            studyGroupId,
            assignment.OperationalPeriodId,
            cancellationToken);

        var participationMap = activeParticipations.ToDictionary(p => p.StudentId);

        foreach (var item in request.Items)
        {
            if (!participationMap.ContainsKey(item.StudentId))
            {
                return Result<EnterStudentScoresResponse>.Failure(ScorebookErrors.StudentNotActivelyEnrolled(item.StudentId));
            }
        }

        var now = DateTime.UtcNow;

        foreach (var item in request.Items)
        {
            var p = participationMap[item.StudentId];
            var scoreResult = assignment.AddOrUpdateScore(
                item.StudentId,
                p.StudentName,
                p.StudentNis,
                item.Score,
                item.Feedback,
                now,
                now);

            if (scoreResult.IsFailure)
            {
                return Result<EnterStudentScoresResponse>.Failure(scoreResult.Error);
            }
        }

        await assignmentRepository.UpsertScoresAsync(assignment.Id, assignment.Scores.ToList(), cancellationToken);

        return Result<EnterStudentScoresResponse>.Success(
            new EnterStudentScoresResponse(assignment.Id, request.Items.Count));
    }
}
