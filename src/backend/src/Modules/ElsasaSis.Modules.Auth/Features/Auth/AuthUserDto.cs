using ElsasaSis.Modules.Auth.Domain;

namespace ElsasaSis.Modules.Auth.Features.Auth;

public sealed record AuthUserDto(
    Guid Id,
    string Username,
    string DisplayName,
    string? Email,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    Guid? TeacherId,
    Guid? StudentId,
    Guid? GuardianId)
{
    public static AuthUserDto FromDomain(AuthUser user, IReadOnlyList<string> permissions) =>
        new(
            user.Id,
            user.Username,
            user.DisplayName,
            user.Email,
            user.Roles,
            permissions,
            user.Actors.TeacherId,
            user.Actors.StudentId,
            user.Actors.GuardianId);
}
