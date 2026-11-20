using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Acd.Domain.Model;

public enum ParticipationStatus
{
    Active = 1,
    Completed = 2
}

public static class ParticipationStatusExtensions
{
    public static string ToCode(this ParticipationStatus status) => status switch
    {
        ParticipationStatus.Active => "ACTIVE",
        ParticipationStatus.Completed => "COMPLETED",
        _ => status.ToString().ToUpperInvariant()
    };

    public static Result<ParticipationStatus> ParseParticipationStatus(string? raw, string field = "status")
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Result<ParticipationStatus>.Failure(ParticipationErrors.InvalidStatus(raw, field));
        }

        var normalized = raw.Trim().ToUpperInvariant();
        return normalized switch
        {
            "ACTIVE" => Result<ParticipationStatus>.Success(ParticipationStatus.Active),
            "COMPLETED" => Result<ParticipationStatus>.Success(ParticipationStatus.Completed),
            _ => Result<ParticipationStatus>.Failure(ParticipationErrors.InvalidStatus(raw, field))
        };
    }
}
