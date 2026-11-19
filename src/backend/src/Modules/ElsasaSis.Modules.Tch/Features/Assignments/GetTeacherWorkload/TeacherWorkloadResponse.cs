namespace ElsasaSis.Modules.Tch.Features.Assignments.GetTeacherWorkload;

public sealed record TeacherWorkloadResponse(
    Guid? OperationalPeriodId,
    string? OperationalPeriodName,
    Guid? AcademicYearId,
    string? AcademicYearName,
    int TotalTeachers,
    int ActiveTeachersCount,
    int AssignedTeachersCount,
    int UnassignedTeachersCount,
    int TotalAssignments,
    int DistinctStudyGroupsCount,
    int DistinctSubjectsCount,
    IReadOnlyList<TeacherWorkloadItemDto> Teachers);

public sealed record TeacherWorkloadItemDto(
    Guid TeacherId,
    string FullName,
    string? Nip,
    string Status,
    int TotalAssignments,
    int DistinctStudyGroupCount,
    int DistinctSubjectCount,
    IReadOnlyList<WorkloadStudyGroupDto> StudyGroups,
    IReadOnlyList<WorkloadSubjectDto> Subjects,
    IReadOnlyList<WorkloadAssignmentItemDto> Assignments);

public sealed record WorkloadStudyGroupDto(
    Guid StudyGroupId,
    string StudyGroupName,
    int AssignmentCount);

public sealed record WorkloadSubjectDto(
    Guid SubjectId,
    string SubjectName,
    int AssignmentCount);

public sealed record WorkloadAssignmentItemDto(
    Guid AssignmentId,
    Guid SubjectId,
    string SubjectName,
    Guid StudyGroupId,
    string StudyGroupName);
