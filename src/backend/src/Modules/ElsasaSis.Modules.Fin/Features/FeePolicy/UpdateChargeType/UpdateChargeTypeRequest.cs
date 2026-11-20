namespace ElsasaSis.Modules.Fin.Features.FeePolicy.UpdateChargeType;

public sealed record UpdateChargeTypeRequest(
    string Name,
    string Frequency,
    decimal Amount,
    string? SchoolLevel = null,
    string? Grade = null,
    Guid? StudyGroupId = null,
    string? StudyGroupName = null);
