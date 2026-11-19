namespace ElsasaSis.Modules.Std.Features.Students.UpdateStudent;

public sealed record UpdateStudentRequest(
    string? Nisn,
    string FullName,
    string? Nickname,
    DateOnly DateOfBirth,
    string Gender,
    string? Phone,
    string? Email,
    string? Address);
