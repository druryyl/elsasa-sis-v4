using ElsasaSis.Modules.Fin.Domain.Model;

namespace ElsasaSis.Modules.Fin.Features.FeePolicy;

public sealed record ChargeTypeDto(
    Guid Id,
    Guid AcademicYearId,
    string AcademicYearName,
    string Name,
    string Frequency,
    decimal Amount,
    string? SchoolLevel,
    string? Grade,
    Guid? StudyGroupId,
    string? StudyGroupName,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static ChargeTypeDto FromDomain(ChargeType chargeType) =>
        new(
            chargeType.Id,
            chargeType.AcademicYearId,
            chargeType.AcademicYearName,
            chargeType.Name,
            chargeType.Frequency,
            chargeType.Amount,
            chargeType.SchoolLevel,
            chargeType.Grade,
            chargeType.StudyGroupId,
            chargeType.StudyGroupName,
            chargeType.Status,
            chargeType.CreatedAt,
            chargeType.UpdatedAt);
}
