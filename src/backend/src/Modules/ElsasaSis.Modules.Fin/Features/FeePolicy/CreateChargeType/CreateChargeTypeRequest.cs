namespace ElsasaSis.Modules.Fin.Features.FeePolicy.CreateChargeType;

public sealed record CreateChargeTypeRequest(
    Guid? AcademicYearId,
    string? AcademicYearName,
    string Name,
    string Frequency,
    decimal Amount,
    string? SchoolLevel = null,
    string? Grade = null,
    Guid? StudyGroupId = null,
    string? StudyGroupName = null);
