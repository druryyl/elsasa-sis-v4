namespace ElsasaSis.SharedKernel.Security;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid UserId { get; }
    string Username { get; }
    string FullName { get; }
    IReadOnlyList<string> Roles { get; }
    IReadOnlyList<string> Permissions { get; }
    Guid? TeacherId { get; }
    Guid? StudentId { get; }
    Guid? GuardianId { get; }
}
