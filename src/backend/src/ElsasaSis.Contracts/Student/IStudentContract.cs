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

public interface IStudentContract
{
    Task<StudyGroupSummaryDto?> GetStudyGroupByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
