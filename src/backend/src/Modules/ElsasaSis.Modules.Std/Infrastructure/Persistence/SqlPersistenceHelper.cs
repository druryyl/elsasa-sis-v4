using System.Data;
using System.Data.Common;
using ElsasaSis.Modules.Std.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Std.Infrastructure.Persistence;

internal static class SqlPersistenceHelper
{
    internal static async Task ExecuteInTransactionAsync(
        ISqlConnectionFactory connectionFactory,
        Func<IDbConnection, IDbTransaction, CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        if (connection is DbConnection dbConnection)
        {
            await using var transaction = await dbConnection.BeginTransactionAsync(cancellationToken);
            try
            {
                await action(connection, transaction, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        else
        {
            using var transaction = connection.BeginTransaction();
            try
            {
                await action(connection, transaction, cancellationToken);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    internal static async Task<TResult> ExecuteResultInTransactionAsync<TResult>(
        ISqlConnectionFactory connectionFactory,
        Func<IDbConnection, IDbTransaction, CancellationToken, Task<TResult>> action,
        Func<DbException, TResult?> uniqueConstraintMapper,
        CancellationToken cancellationToken)
        where TResult : Result
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        if (connection is DbConnection dbConnection)
        {
            await using var transaction = await dbConnection.BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await action(connection, transaction, cancellationToken);
                if (result.IsSuccess)
                {
                    await transaction.CommitAsync(cancellationToken);
                }
                else
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                return result;
            }
            catch (DbException ex) when (IsUniqueConstraintViolation(ex))
            {
                await transaction.RollbackAsync(cancellationToken);
                var mapped = uniqueConstraintMapper(ex);
                if (mapped is not null)
                {
                    return mapped;
                }

                throw;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        using var syncTransaction = connection.BeginTransaction();
        try
        {
            var result = await action(connection, syncTransaction, cancellationToken);
            if (result.IsSuccess)
            {
                syncTransaction.Commit();
            }
            else
            {
                syncTransaction.Rollback();
            }

            return result;
        }
        catch (DbException ex) when (IsUniqueConstraintViolation(ex))
        {
            syncTransaction.Rollback();
            var mapped = uniqueConstraintMapper(ex);
            if (mapped is not null)
            {
                return mapped;
            }

            throw;
        }
        catch
        {
            syncTransaction.Rollback();
            throw;
        }
    }

    internal static bool IsUniqueConstraintViolation(DbException exception)
    {
        if (!string.Equals(exception.GetType().Name, "SqlException", StringComparison.Ordinal))
        {
            return false;
        }

        var numberProperty = exception.GetType().GetProperty("Number");
        if (numberProperty?.GetValue(exception) is int number)
        {
            return number is 2601 or 2627;
        }

        return false;
    }

    internal static DateTime SpecifyUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    internal static DateTime? SpecifyNullableUtc(DateTime? value) =>
        value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null;
}

internal static class StudentPersistenceShared
{
    internal const string InsertStudentSql = """
        INSERT INTO std.students (
            id, candidate_id, nis, nisn, full_name, nickname, date_of_birth, gender,
            phone, email, address, enrollment_date, status, created_at, updated_at
        )
        VALUES (
            @Id, @CandidateId, @Nis, @Nisn, @FullName, @Nickname, @DateOfBirth, @Gender,
            @Phone, @Email, @Address, @EnrollmentDate, @Status, @CreatedAt, @UpdatedAt
        );
        """;

    internal const string InsertStudentGuardianSql = """
        INSERT INTO std.student_guardians (
            id, student_id, full_name, relationship_type, phone, email, address, created_at, updated_at
        )
        VALUES (
            @Id, @StudentId, @FullName, @RelationshipType, @Phone, @Email, @Address, @CreatedAt, @UpdatedAt
        );
        """;

    internal const string ExistsOtherNisWithLockSql = """
        SELECT CASE WHEN EXISTS (
            SELECT 1
            FROM std.students WITH (UPDLOCK, HOLDLOCK)
            WHERE nis = @Nis
              AND (@ExcludeStudentId IS NULL OR id <> @ExcludeStudentId)
        ) THEN 1 ELSE 0 END;
        """;

    internal const string ExistsOtherNisnWithLockSql = """
        SELECT CASE WHEN EXISTS (
            SELECT 1
            FROM std.students WITH (UPDLOCK, HOLDLOCK)
            WHERE nisn = @Nisn
              AND (@ExcludeStudentId IS NULL OR id <> @ExcludeStudentId)
        ) THEN 1 ELSE 0 END;
        """;

    internal static object ToStudentParameters(Student student) => new
    {
        student.Id,
        student.CandidateId,
        student.Nis,
        student.Nisn,
        student.FullName,
        student.Nickname,
        DateOfBirth = student.DateOfBirth.ToDateTime(TimeOnly.MinValue),
        Gender = student.Gender.ToCode(),
        student.Phone,
        student.Email,
        student.Address,
        EnrollmentDate = student.EnrollmentDate.ToDateTime(TimeOnly.MinValue),
        Status = student.Status.ToCode(),
        student.CreatedAt,
        student.UpdatedAt
    };

    internal static object ToStudentGuardianParameters(StudentGuardian guardian) => new
    {
        guardian.Id,
        guardian.StudentId,
        guardian.FullName,
        RelationshipType = guardian.RelationshipType.ToCode(),
        guardian.Phone,
        guardian.Email,
        guardian.Address,
        guardian.CreatedAt,
        guardian.UpdatedAt
    };

    internal static DomainError? MapStudentUniqueConstraintError(DbException ex, string nis, string? nisn)
    {
        // Check UQ_students_nisn before UQ_students_nis because the former starts with the latter.
        if (ex.Message.Contains("UQ_students_nisn", StringComparison.OrdinalIgnoreCase))
        {
            return StdErrors.DuplicateNisn(nisn ?? string.Empty);
        }

        if (ex.Message.Contains("UQ_students_nis", StringComparison.OrdinalIgnoreCase))
        {
            return StdErrors.DuplicateNis(nis);
        }

        return null;
    }
}
