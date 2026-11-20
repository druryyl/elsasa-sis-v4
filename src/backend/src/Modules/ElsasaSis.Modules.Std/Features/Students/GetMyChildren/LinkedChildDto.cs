namespace ElsasaSis.Modules.Std.Features.Students.GetMyChildren;

public sealed record LinkedChildDto
{
    public Guid StudentId { get; init; }
    public string Nis { get; init; } = string.Empty;
    public string? Nisn { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? Nickname { get; init; }
    public DateTime DateOfBirth { get; init; }
    public string Gender { get; init; } = string.Empty;
    public Guid? StudyGroupId { get; init; }
    public string? StudyGroupName { get; init; }
    public string? SchoolLevel { get; init; }
    public string? Grade { get; init; }

    public LinkedChildDto() { }

    public LinkedChildDto(
        Guid studentId,
        string nis,
        string? nisn,
        string fullName,
        string? nickname,
        DateTime dateOfBirth,
        string gender,
        Guid? studyGroupId = null,
        string? studyGroupName = null,
        string? schoolLevel = null,
        string? grade = null)
    {
        StudentId = studentId;
        Nis = nis;
        Nisn = nisn;
        FullName = fullName;
        Nickname = nickname;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        StudyGroupId = studyGroupId;
        StudyGroupName = studyGroupName;
        SchoolLevel = schoolLevel;
        Grade = grade;
    }
}
