namespace ElsasaSis.Modules.Auth.Domain;

public sealed record UserActors(
    Guid? TeacherId,
    Guid? StudentId,
    Guid? GuardianId)
{
    public static UserActors Empty { get; } = new(null, null, null);
}
