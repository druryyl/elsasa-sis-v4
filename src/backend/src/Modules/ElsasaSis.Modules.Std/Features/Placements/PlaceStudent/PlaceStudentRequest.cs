namespace ElsasaSis.Modules.Std.Features.Placements.PlaceStudent;

public sealed record PlaceStudentRequest(
    Guid AcademicYearId,
    string SchoolLevel,
    string Grade,
    Guid? StudyGroupId);

public sealed record PlaceStudentFlatRequest(
    Guid StudentId,
    Guid AcademicYearId,
    string SchoolLevel,
    string Grade,
    Guid? StudyGroupId);
