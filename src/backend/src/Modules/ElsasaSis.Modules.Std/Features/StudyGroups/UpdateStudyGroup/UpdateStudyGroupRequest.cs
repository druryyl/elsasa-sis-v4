namespace ElsasaSis.Modules.Std.Features.StudyGroups.UpdateStudyGroup;

public sealed record UpdateStudyGroupRequest(
    Guid AcademicYearId,
    string SchoolLevel,
    string Grade,
    string Name,
    Guid? HomeroomTeacherId);
