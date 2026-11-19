namespace ElsasaSis.Modules.Tch.Features.Assignments.AssignTeacher;

public sealed record AssignTeacherRequest(
    Guid TeacherId,
    Guid OperationalPeriodId,
    Guid SubjectId,
    Guid StudyGroupId);
