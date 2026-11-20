using ElsasaSis.Modules.Fin.Domain.Model;

namespace ElsasaSis.Modules.Fin.Domain;

public interface IStudentDiscountRepository
{
    Task<StudentDiscount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentDiscount>> GetByStudentIdAsync(Guid studentId, Guid academicYearId, CancellationToken cancellationToken = default);
    Task<StudentDiscount?> GetActiveDiscountAsync(Guid studentId, Guid chargeTypeId, Guid academicYearId, CancellationToken cancellationToken = default);
    Task<bool> ExistsForStudentAndChargeTypeAsync(Guid studentId, Guid chargeTypeId, Guid academicYearId, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentDiscount>> GetByChargeTypeIdAsync(Guid chargeTypeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentDiscount>> GetDiscountsAsync(
        Guid? academicYearId = null,
        Guid? studentId = null,
        Guid? chargeTypeId = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);
    Task InsertAsync(StudentDiscount discount, CancellationToken cancellationToken = default);
    Task UpdateAsync(StudentDiscount discount, CancellationToken cancellationToken = default);
}
