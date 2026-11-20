using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Fin.Features.Billing.GetStudentCharges;

public sealed class GetStudentChargesHandler(IStudentChargeRepository studentChargeRepository)
{
    public async Task<Result<IReadOnlyList<StudentChargeDto>>> HandleAsync(
        GetStudentChargesQuery query,
        CancellationToken cancellationToken = default)
    {
        var charges = await studentChargeRepository.GetChargesAsync(
            academicYearId: query.AcademicYearId,
            billingMonth: query.BillingMonth,
            schoolLevel: query.SchoolLevel,
            grade: query.Grade,
            studyGroupId: query.StudyGroupId,
            studentId: query.StudentId,
            status: query.Status,
            cancellationToken: cancellationToken);

        var dtos = charges.Select(StudentChargeDto.FromDomain).ToList();
        return Result<IReadOnlyList<StudentChargeDto>>.Success(dtos);
    }
}
