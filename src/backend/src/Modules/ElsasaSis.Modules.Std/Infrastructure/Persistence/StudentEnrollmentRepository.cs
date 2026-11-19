using System.Data;
using System.Globalization;
using Dapper;
using ElsasaSis.Modules.Std.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Std.Infrastructure.Persistence;

public sealed class StudentEnrollmentRepository(ISqlConnectionFactory connectionFactory) : IStudentEnrollmentRepository
{
    private const string NextSequentialNisSql = """
        SELECT COALESCE(MAX(CAST(RIGHT(nis, 4) AS INT)), 0) + 1
        FROM std.students
        WHERE LEN(nis) = 8
          AND nis LIKE @YearPrefix + '[0-9][0-9][0-9][0-9]';
        """;

    private const string NextSequentialNisWithLockSql = """
        SELECT COALESCE(MAX(CAST(RIGHT(nis, 4) AS INT)), 0) + 1
        FROM std.students WITH (UPDLOCK, HOLDLOCK)
        WHERE LEN(nis) = 8
          AND nis LIKE @YearPrefix + '[0-9][0-9][0-9][0-9]';
        """;

    private const string MarkCandidateEnrolledSql = """
        UPDATE std.candidates
        SET admission_status = @AdmissionStatus,
            enrolled_student_id = @EnrolledStudentId,
            updated_at = @UpdatedAt
        WHERE id = @Id
          AND admission_status = 'ACCEPTED';
        """;

    public async Task<string> GetNextNisAsync(int year, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var yearPrefix = year.ToString("0000", CultureInfo.InvariantCulture);

        var nextSequence = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            NextSequentialNisSql,
            new { YearPrefix = yearPrefix },
            cancellationToken: cancellationToken));

        return FormatSequentialNis(yearPrefix, nextSequence);
    }

    public Task<Result<Student>> EnrollCandidateAsync(
        Candidate candidate,
        string? requestedNis,
        string? nisn,
        DateOnly? enrollmentDate,
        DateTime? utcNow = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        if (candidate.AdmissionStatus == AdmissionStatus.Enrolled)
        {
            return Task.FromResult(Result<Student>.Failure(StdErrors.CandidateAlreadyEnrolled(candidate.Id)));
        }

        if (candidate.AdmissionStatus != AdmissionStatus.Accepted)
        {
            return Task.FromResult(Result<Student>.Failure(
                StdErrors.CandidateNotAcceptedForEnrollment(candidate.Id, candidate.AdmissionStatus)));
        }

        if (enrollmentDate.HasValue && enrollmentDate.Value == default)
        {
            return Task.FromResult(Result<Student>.Failure(StdErrors.EnrollmentDateInvalid()));
        }

        var now = utcNow ?? DateTime.UtcNow;
        var effectiveEnrollmentDate = enrollmentDate ?? DateOnly.FromDateTime(now);
        var normalizedNisn = string.IsNullOrWhiteSpace(nisn) ? null : nisn.Trim();
        string? resolvedNis = string.IsNullOrWhiteSpace(requestedNis) ? null : requestedNis.Trim();

        return SqlPersistenceHelper.ExecuteResultInTransactionAsync(
            connectionFactory,
            async (connection, transaction, ct) =>
            {
                if (resolvedNis is null)
                {
                    var yearPrefix = effectiveEnrollmentDate.Year.ToString("0000", CultureInfo.InvariantCulture);
                    var nextSequence = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                        NextSequentialNisWithLockSql,
                        new { YearPrefix = yearPrefix },
                        transaction,
                        cancellationToken: ct));

                    resolvedNis = FormatSequentialNis(yearPrefix, nextSequence);
                }
                else
                {
                    var nisExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                        StudentPersistenceShared.ExistsOtherNisWithLockSql,
                        new { Nis = resolvedNis, ExcludeStudentId = (Guid?)null },
                        transaction,
                        cancellationToken: ct));

                    if (nisExists)
                    {
                        return Result<Student>.Failure(StdErrors.DuplicateNis(resolvedNis));
                    }
                }

                if (normalizedNisn is not null)
                {
                    var nisnExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                        StudentPersistenceShared.ExistsOtherNisnWithLockSql,
                        new { Nisn = normalizedNisn, ExcludeStudentId = (Guid?)null },
                        transaction,
                        cancellationToken: ct));

                    if (nisnExists)
                    {
                        return Result<Student>.Failure(StdErrors.DuplicateNisn(normalizedNisn));
                    }
                }

                var enrollResult = Student.EnrollFromCandidate(
                    candidate,
                    resolvedNis,
                    normalizedNisn,
                    effectiveEnrollmentDate,
                    now);

                if (enrollResult.IsFailure)
                {
                    return enrollResult;
                }

                var student = enrollResult.Value!;
                var persistResult = await PersistEnrollmentRowsAsync(connection, transaction, student, candidate, ct);
                return persistResult.IsFailure
                    ? Result<Student>.Failure(persistResult.Error)
                    : Result<Student>.Success(student);
            },
            ex =>
            {
                var error = StudentPersistenceShared.MapStudentUniqueConstraintError(ex, resolvedNis ?? string.Empty, normalizedNisn);
                return error is null ? null : Result<Student>.Failure(error);
            },
            cancellationToken);
    }

    public Task<Result> EnrollAsync(
        Student student,
        Candidate candidate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(student);
        ArgumentNullException.ThrowIfNull(candidate);

        return SqlPersistenceHelper.ExecuteResultInTransactionAsync(
            connectionFactory,
            async (connection, transaction, ct) =>
            {
                var nisExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                    StudentPersistenceShared.ExistsOtherNisWithLockSql,
                    new { student.Nis, ExcludeStudentId = (Guid?)null },
                    transaction,
                    cancellationToken: ct));

                if (nisExists)
                {
                    return Result.Failure(StdErrors.DuplicateNis(student.Nis));
                }

                if (student.Nisn is not null)
                {
                    var nisnExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                        StudentPersistenceShared.ExistsOtherNisnWithLockSql,
                        new { student.Nisn, ExcludeStudentId = (Guid?)null },
                        transaction,
                        cancellationToken: ct));

                    if (nisnExists)
                    {
                        return Result.Failure(StdErrors.DuplicateNisn(student.Nisn));
                    }
                }

                return await PersistEnrollmentRowsAsync(connection, transaction, student, candidate, ct);
            },
            ex =>
            {
                var error = StudentPersistenceShared.MapStudentUniqueConstraintError(ex, student.Nis, student.Nisn);
                return error is null ? null : Result.Failure(error);
            },
            cancellationToken);
    }

    private static async Task<Result> PersistEnrollmentRowsAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        Student student,
        Candidate candidate,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(new CommandDefinition(
            StudentPersistenceShared.InsertStudentSql,
            StudentPersistenceShared.ToStudentParameters(student),
            transaction,
            cancellationToken: cancellationToken));

        foreach (var guardian in student.Guardians)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                StudentPersistenceShared.InsertStudentGuardianSql,
                StudentPersistenceShared.ToStudentGuardianParameters(guardian),
                transaction,
                cancellationToken: cancellationToken));
        }

        var affectedRows = await connection.ExecuteAsync(new CommandDefinition(
            MarkCandidateEnrolledSql,
            new
            {
                candidate.Id,
                AdmissionStatus = candidate.AdmissionStatus.ToCode(),
                EnrolledStudentId = student.Id,
                candidate.UpdatedAt
            },
            transaction,
            cancellationToken: cancellationToken));

        if (affectedRows == 0)
        {
            return Result.Failure(StdErrors.CandidateAlreadyEnrolled(candidate.Id));
        }

        return Result.Success();
    }

    private static string FormatSequentialNis(string yearPrefix, int sequence) =>
        $"{yearPrefix}{sequence.ToString("D4", CultureInfo.InvariantCulture)}";
}
