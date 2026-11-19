namespace ElsasaSis.Modules.Tch.Features.Teachers.CreateTeacher;

public sealed record CreateTeacherRequest(
    string FullName,
    string? Nickname,
    string? Nip,
    string? Nik,
    DateOnly DateOfBirth,
    string Gender,
    string? Phone,
    string? Email,
    string? Address) : ITeacherProfileRequest;
