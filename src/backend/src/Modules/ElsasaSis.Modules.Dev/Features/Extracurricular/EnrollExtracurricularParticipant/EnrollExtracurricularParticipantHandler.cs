using ElsasaSis.Contracts.Operational;
using ElsasaSis.Contracts.Student;
using ElsasaSis.Modules.Dev.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Dev.Features.Extracurricular.EnrollExtracurricularParticipant;

public sealed class EnrollExtracurricularParticipantHandler(
    IExtracurricularActivityRepository repository,
    IStudentContract studentContract,
    IOperationalContract operationalContract)
{
    public async Task<Result<ExtracurricularParticipationDto>> HandleAsync(
        Guid activityId,
        EnrollExtracurricularParticipantRequest request,
        CancellationToken cancellationToken = default)
    {
        var activity = await repository.GetByIdAsync(activityId, cancellationToken);
        if (activity is null)
        {
            return Result<ExtracurricularParticipationDto>.Failure(DevErrors.ExtracurricularNotFound(activityId));
        }

        if (activity.Status != ExtracurricularActivityStatus.Active)
        {
            return Result<ExtracurricularParticipationDto>.Failure(DevErrors.ActivityNotActive(activityId));
        }

        Guid academicYearId;
        string academicYearName;
        if (request.AcademicYearId.HasValue && request.AcademicYearId.Value != Guid.Empty)
        {
            academicYearId = request.AcademicYearId.Value;
            if (!string.IsNullOrWhiteSpace(request.AcademicYearName))
            {
                academicYearName = request.AcademicYearName.Trim();
            }
            else
            {
                var academicYear = await operationalContract.GetAcademicYearByIdAsync(academicYearId, cancellationToken);
                if (academicYear is null)
                {
                    return Result<ExtracurricularParticipationDto>.Failure(DevErrors.AcademicYearNotFound(academicYearId));
                }
                academicYearName = academicYear.Name;
            }
        }
        else
        {
            var activeAcademicYear = await operationalContract.GetActiveAcademicYearAsync(cancellationToken);
            if (activeAcademicYear is null)
            {
                return Result<ExtracurricularParticipationDto>.Failure(DevErrors.NoActiveAcademicYear());
            }
            academicYearId = activeAcademicYear.Id;
            academicYearName = activeAcademicYear.Name;
        }

        var studentName = request.StudentName?.Trim() ?? string.Empty;
        var studentNis = request.StudentNis?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(studentName) || string.IsNullOrWhiteSpace(studentNis))
        {
            var activeStudents = await studentContract.GetActiveStudentsByScopeAsync(
                studyGroupId: null,
                schoolLevel: null,
                grade: null,
                cancellationToken);

            var student = activeStudents.FirstOrDefault(s => s.StudentId == request.StudentId);
            if (student is null)
            {
                return Result<ExtracurricularParticipationDto>.Failure(DevErrors.StudentNotFound(request.StudentId));
            }

            studentName = student.StudentName;
            studentNis = student.StudentNis;
        }

        var enrollResult = activity.EnrollStudent(
            request.StudentId,
            studentName,
            studentNis,
            academicYearId,
            academicYearName,
            request.JoinedDate);

        if (enrollResult.IsFailure)
        {
            return Result<ExtracurricularParticipationDto>.Failure(enrollResult.Error);
        }

        var participation = enrollResult.Value!;
        await repository.InsertParticipationAsync(participation, cancellationToken);
        await repository.UpdateAsync(activity, cancellationToken);

        return Result<ExtracurricularParticipationDto>.Success(ExtracurricularParticipationDto.FromDomain(participation));
    }
}
