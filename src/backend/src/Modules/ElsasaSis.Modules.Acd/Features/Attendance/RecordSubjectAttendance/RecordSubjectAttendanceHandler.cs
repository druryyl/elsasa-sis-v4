using ElsasaSis.Contracts.Academic;
using ElsasaSis.Contracts.Operational;
using ElsasaSis.Contracts.Student;
using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.Modules.Acd.Domain.Model;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Security;

namespace ElsasaSis.Modules.Acd.Features.Attendance.RecordSubjectAttendance;

public sealed class RecordSubjectAttendanceHandler(
    IAttendanceRecordRepository attendanceRecordRepository,
    IOperationalContract operationalContract,
    IAcademicContract academicContract,
    IStudentContract studentContract,
    ICurrentUser currentUser)
{
    public async Task<Result<RecordAttendanceResponse>> HandleAsync(
        RecordAttendanceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Items is null || request.Items.Count == 0)
        {
            return Result<RecordAttendanceResponse>.Failure(AttendanceErrors.ItemsRequired());
        }

        // Check for duplicate student IDs in the batch payload
        var seenStudentIds = new HashSet<Guid>();
        foreach (var item in request.Items)
        {
            if (!seenStudentIds.Add(item.StudentId))
            {
                return Result<RecordAttendanceResponse>.Failure(AttendanceErrors.DuplicateStudentInBatch(item.StudentId));
            }
        }

        var period = await operationalContract.GetOperationalPeriodByIdAsync(request.OperationalPeriodId, cancellationToken);
        if (period is null)
        {
            return Result<RecordAttendanceResponse>.Failure(AttendanceErrors.OperationalPeriodNotFound(request.OperationalPeriodId));
        }

        var subject = await academicContract.GetSubjectByIdAsync(request.SubjectId, cancellationToken);
        if (subject is null)
        {
            return Result<RecordAttendanceResponse>.Failure(AttendanceErrors.SubjectNotFound(request.SubjectId));
        }

        var studyGroup = await studentContract.GetStudyGroupByIdAsync(request.StudyGroupId, cancellationToken);
        if (studyGroup is null)
        {
            return Result<RecordAttendanceResponse>.Failure(AttendanceErrors.StudyGroupNotFound(request.StudyGroupId));
        }

        var enrolledStudents = await attendanceRecordRepository.GetEnrolledStudentsAsync(
            request.OperationalPeriodId,
            request.SubjectId,
            request.StudyGroupId,
            cancellationToken);

        var enrolledMap = enrolledStudents.ToDictionary(s => s.StudentId);

        // Validate that each student is actively enrolled in this subject and study group
        foreach (var item in request.Items)
        {
            if (!enrolledMap.ContainsKey(item.StudentId))
            {
                return Result<RecordAttendanceResponse>.Failure(AttendanceErrors.StudentNotEnrolled(item.StudentId));
            }
        }

        // Determine recording teacher identity
        var teacherId = currentUser.TeacherId ?? (currentUser.UserId != Guid.Empty ? currentUser.UserId : Guid.Parse("01A1F11A-700B-700B-8000-00000000000B"));
        var teacherName = !string.IsNullOrWhiteSpace(currentUser.FullName)
            ? currentUser.FullName
            : (!string.IsNullOrWhiteSpace(currentUser.Username) ? currentUser.Username : "Teacher");

        // Load any existing attendance records for this subject, study group, and date
        var existingRecords = await attendanceRecordRepository.GetBySubjectGroupAndDateAsync(
            request.SubjectId,
            request.StudyGroupId,
            request.AttendanceDate,
            cancellationToken);

        var existingMap = existingRecords.ToDictionary(r => r.StudentId);
        var recordsToUpsert = new List<AttendanceRecord>(request.Items.Count);
        var now = DateTime.UtcNow;

        foreach (var item in request.Items)
        {
            var statusResult = AttendanceStatusExtensions.ParseAttendanceStatus(item.Status);
            if (statusResult.IsFailure)
            {
                return Result<RecordAttendanceResponse>.Failure(statusResult.Error);
            }

            var status = statusResult.Value;
            var enrolledInfo = enrolledMap[item.StudentId];

            if (existingMap.TryGetValue(item.StudentId, out var existing))
            {
                var updateResult = existing.UpdateStatus(
                    status,
                    item.Notes,
                    teacherId,
                    teacherName,
                    now);

                if (updateResult.IsFailure)
                {
                    return Result<RecordAttendanceResponse>.Failure(updateResult.Error);
                }

                recordsToUpsert.Add(existing);
            }
            else
            {
                var createResult = AttendanceRecord.Create(
                    enrolledInfo.StudentId,
                    enrolledInfo.StudentName,
                    enrolledInfo.StudentNis,
                    subject.Id,
                    subject.Name,
                    studyGroup.Id,
                    studyGroup.Name,
                    period.Id,
                    period.Name,
                    request.AttendanceDate,
                    status,
                    item.Notes,
                    teacherId,
                    teacherName,
                    now);

                if (createResult.IsFailure)
                {
                    return Result<RecordAttendanceResponse>.Failure(createResult.Error);
                }

                recordsToUpsert.Add(createResult.Value!);
            }
        }

        var upsertResult = await attendanceRecordRepository.UpsertBatchAsync(recordsToUpsert, cancellationToken);
        if (upsertResult.IsFailure)
        {
            return Result<RecordAttendanceResponse>.Failure(upsertResult.Error);
        }

        return Result<RecordAttendanceResponse>.Success(new RecordAttendanceResponse(recordsToUpsert.Count));
    }
}
