namespace ElsasaSis.Contracts.Student;

public sealed record StudyGroupSummaryDto(
    Guid Id,
    Guid AcademicYearId,
    string AcademicYearName,
    string SchoolLevel,
    string Grade,
    string Name,
    Guid? HomeroomTeacherId,
    string? HomeroomTeacherName);

public sealed record StudentPlacementScopeDto(
    Guid StudentId,
    string StudentName,
    string StudentNis,
    Guid StudyGroupId,
    string StudyGroupName,
    string SchoolLevel,
    string Grade);

public interface IStudentContract
{
    Task<StudyGroupSummaryDto?> GetStudyGroupByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentPlacementScopeDto>> GetActiveStudentsByScopeAsync(
        Guid? studyGroupId,
        string? schoolLevel,
        string? grade,
        CancellationToken cancellationToken = default);
}
