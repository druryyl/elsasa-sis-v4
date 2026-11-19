namespace ElsasaSis.Modules.Tch.Features.Teachers.UpdateTeacher;

public sealed record UpdateTeacherRequest(
    string FullName,
    string? Nickname,
    string? Nip,
    string? Nik,
    DateOnly DateOfBirth,
    string Gender,
    string? Phone,
    string? Email,
    string? Address) : ITeacherProfileRequest;
