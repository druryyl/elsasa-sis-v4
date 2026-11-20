using ElsasaSis.Modules.Acd.Domain.Model;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Acd.Domain;

public sealed record EnrolledStudentSnapshotDto(
    Guid StudentId,
    string StudentName,
    string StudentNis);

public interface IAttendanceRecordRepository
{
    Task<AttendanceRecord?> GetByStudentSubjectAndDateAsync(
        Guid studentId,
        Guid subjectId,
        DateOnly attendanceDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttendanceRecord>> GetBySubjectGroupAndDateAsync(
        Guid subjectId,
        Guid studyGroupId,
        DateOnly attendanceDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnrolledStudentSnapshotDto>> GetEnrolledStudentsAsync(
        Guid operationalPeriodId,
        Guid subjectId,
        Guid studyGroupId,
        CancellationToken cancellationToken = default);

    Task<Result> UpsertBatchAsync(
        IReadOnlyList<AttendanceRecord> records,
        CancellationToken cancellationToken = default);
}
