using ElsasaSis.Modules.Fin.Domain.Model;

namespace ElsasaSis.Modules.Fin.Domain;

public interface IChargeTypeRepository
{
    Task<ChargeType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChargeType>> GetByAcademicYearAsync(Guid? academicYearId = null, string? status = null, string? frequency = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChargeType>> GetActiveByScopeAsync(Guid academicYearId, string? schoolLevel = null, string? grade = null, Guid? studyGroupId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameInYearAsync(string name, Guid academicYearId, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task InsertAsync(ChargeType chargeType, CancellationToken cancellationToken = default);
    Task UpdateAsync(ChargeType chargeType, CancellationToken cancellationToken = default);
}
