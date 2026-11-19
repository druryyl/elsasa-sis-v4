namespace ElsasaSis.Modules.Std.Features.StudyGroups.CreateStudyGroup;

public sealed record CreateStudyGroupRequest(
    Guid AcademicYearId,
    string SchoolLevel,
    string Grade,
    string Name,
    Guid? HomeroomTeacherId);
