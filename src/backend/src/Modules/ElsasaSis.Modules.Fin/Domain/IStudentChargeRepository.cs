using ElsasaSis.Modules.Fin.Domain.Model;

namespace ElsasaSis.Modules.Fin.Domain;

public interface IStudentChargeRepository
{
    Task<StudentCharge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentCharge>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentCharge>> GetByStudentIdAsync(Guid studentId, string? status = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentCharge>> GetUnpaidOrPartiallyPaidByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentCharge>> GetChargesAsync(
        Guid? academicYearId = null,
        string? billingMonth = null,
        string? schoolLevel = null,
        string? grade = null,
        Guid? studyGroupId = null,
        Guid? studentId = null,
        string? status = null,
        CancellationToken cancellationToken = default);
    Task<bool> ExistsMonthlyChargeAsync(Guid studentId, Guid chargeTypeId, string billingMonth, CancellationToken cancellationToken = default);
    Task<bool> ExistsYearlyChargeAsync(Guid studentId, Guid chargeTypeId, Guid academicYearId, CancellationToken cancellationToken = default);
    Task<bool> ExistsSemesterChargeAsync(Guid studentId, Guid chargeTypeId, Guid operationalPeriodId, CancellationToken cancellationToken = default);
    Task InsertAsync(StudentCharge charge, CancellationToken cancellationToken = default);
    Task InsertBatchAsync(IEnumerable<StudentCharge> charges, CancellationToken cancellationToken = default);
    Task UpdateAsync(StudentCharge charge, CancellationToken cancellationToken = default);
}
