using System.Data.Common;
using Dapper;
using ElsasaSis.Modules.Tch.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Tch.Infrastructure.Persistence;

public sealed class TeacherRepository(ISqlConnectionFactory connectionFactory) : ITeacherRepository
{
    private const string SelectTeacherSql = """
        SELECT id, full_name, nickname, nip, nik, date_of_birth, gender, phone, email, address, status, created_at, updated_at
        FROM tch.teachers
        WHERE id = @Id;
        """;

    private const string SelectCapabilitiesSql = """
        SELECT id, teacher_id, subject_id, subject_name, created_at, updated_at
        FROM tch.teaching_capabilities
        WHERE teacher_id = @TeacherId
        ORDER BY subject_name ASC;
        """;

    private const string ExistsByNipSql = """
        SELECT CASE WHEN EXISTS (
            SELECT 1
            FROM tch.teachers
            WHERE nip = @Nip
              AND (@ExcludeId IS NULL OR id <> @ExcludeId)
        ) THEN 1 ELSE 0 END;
        """;

    private const string ExistsByNikSql = """
        SELECT CASE WHEN EXISTS (
            SELECT 1
            FROM tch.teachers
            WHERE nik = @Nik
              AND (@ExcludeId IS NULL OR id <> @ExcludeId)
        ) THEN 1 ELSE 0 END;
        """;

    private const string CountActiveAssignmentsSql = """
        SELECT COUNT(1)
        FROM tch.teacher_assignments
        WHERE teacher_id = @TeacherId
          AND status = 'ASSIGNED';
        """;

    private const string InsertTeacherSql = """
        INSERT INTO tch.teachers (
            id, full_name, nickname, nip, nik, date_of_birth, gender, phone, email, address, status, created_at, updated_at
        )
        VALUES (
            @Id, @FullName, @Nickname, @Nip, @Nik, @DateOfBirth, @Gender, @Phone, @Email, @Address, @Status, @CreatedAt, @UpdatedAt
        );
        """;

    private const string SelectCurrentFullNameSql = """
        SELECT full_name
        FROM tch.teachers
        WHERE id = @Id;
        """;

    private const string UpdateTeacherSql = """
        UPDATE tch.teachers
        SET full_name = @FullName,
            nickname = @Nickname,
            nip = @Nip,
            nik = @Nik,
            date_of_birth = @DateOfBirth,
            gender = @Gender,
            phone = @Phone,
            email = @Email,
            address = @Address,
            status = @Status,
            updated_at = @UpdatedAt
        WHERE id = @Id;
        """;

    // EXT-TCH-004: Keep teacher_name snapshot synchronized on non-removed assignments.
    private const string RefreshAssignmentTeacherNameSql = """
        UPDATE tch.teacher_assignments
        SET teacher_name = @TeacherName,
            updated_at = @UpdatedAt
        WHERE teacher_id = @TeacherId
          AND status <> 'REMOVED';
        """;

    private const string DeleteCapabilitiesByTeacherSql = """
        DELETE FROM tch.teaching_capabilities
        WHERE teacher_id = @TeacherId;
        """;

    private const string InsertCapabilitySql = """
        INSERT INTO tch.teaching_capabilities (
            id, teacher_id, subject_id, subject_name, created_at, updated_at
        )
        VALUES (
            @Id, @TeacherId, @SubjectId, @SubjectName, @CreatedAt, @UpdatedAt
        );
        """;

    public async Task<Teacher?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<TeacherRow>(
            new CommandDefinition(SelectTeacherSql, new { Id = id }, cancellationToken: cancellationToken));

        if (row is null)
        {
            return null;
        }

        var capabilityRows = await connection.QueryAsync<TeachingCapabilityRow>(
            new CommandDefinition(SelectCapabilitiesSql, new { TeacherId = id }, cancellationToken: cancellationToken));

        var capabilities = capabilityRows.Select(c => TeachingCapability.Rehydrate(
            c.id,
            c.teacher_id,
            c.subject_id,
            c.subject_name,
            c.created_at,
            c.updated_at));

        return Teacher.Rehydrate(
            row.id,
            row.full_name,
            row.nickname,
            row.nip,
            row.nik,
            DateOnly.FromDateTime(row.date_of_birth),
            Enum.Parse<Gender>(row.gender, ignoreCase: true),
            row.phone,
            row.email,
            row.address,
            Enum.Parse<TeacherStatus>(row.status, ignoreCase: true),
            row.created_at,
            row.updated_at,
            capabilities);
    }

    public async Task<bool> ExistsByNipAsync(string nip, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nip))
        {
            return false;
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(ExistsByNipSql, new { Nip = nip.Trim(), ExcludeId = excludeId }, cancellationToken: cancellationToken));
    }

    public async Task<bool> ExistsByNikAsync(string nik, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nik))
        {
            return false;
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(ExistsByNikSql, new { Nik = nik.Trim(), ExcludeId = excludeId }, cancellationToken: cancellationToken));
    }

    public async Task<int> CountActiveAssignmentsAsync(Guid teacherId, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(CountActiveAssignmentsSql, new { TeacherId = teacherId }, cancellationToken: cancellationToken));
    }

    public async Task<Result> AddAsync(Teacher teacher, CancellationToken cancellationToken = default)
    {
        try
        {
            await SqlPersistenceHelper.ExecuteInTransactionAsync(connectionFactory, async (connection, transaction, ct) =>
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    InsertTeacherSql,
                    ToTeacherParameters(teacher),
                    transaction,
                    cancellationToken: ct));

                foreach (var capability in teacher.Capabilities)
                {
                    await connection.ExecuteAsync(new CommandDefinition(
                        InsertCapabilitySql,
                        ToCapabilityParameters(capability),
                        transaction,
                        cancellationToken: ct));
                }
            }, cancellationToken);

            return Result.Success();
        }
        catch (DbException ex) when (TryMapUniqueViolation(ex, teacher, out var error))
        {
            return Result.Failure(error);
        }
    }

    public async Task<Result> UpdateAsync(Teacher teacher, CancellationToken cancellationToken = default)
    {
        try
        {
            await SqlPersistenceHelper.ExecuteInTransactionAsync(connectionFactory, async (connection, transaction, ct) =>
            {
                var previousFullName = await connection.ExecuteScalarAsync<string?>(new CommandDefinition(
                    SelectCurrentFullNameSql,
                    new { teacher.Id },
                    transaction,
                    cancellationToken: ct));

                await connection.ExecuteAsync(new CommandDefinition(
                    UpdateTeacherSql,
                    ToTeacherParameters(teacher),
                    transaction,
                    cancellationToken: ct));

                if (previousFullName is not null &&
                    !string.Equals(previousFullName, teacher.FullName, StringComparison.Ordinal))
                {
                    await connection.ExecuteAsync(new CommandDefinition(
                        RefreshAssignmentTeacherNameSql,
                        new
                        {
                            TeacherId = teacher.Id,
                            TeacherName = teacher.FullName,
                            teacher.UpdatedAt
                        },
                        transaction,
                        cancellationToken: ct));
                }

                await connection.ExecuteAsync(new CommandDefinition(
                    DeleteCapabilitiesByTeacherSql,
                    new { TeacherId = teacher.Id },
                    transaction,
                    cancellationToken: ct));

                foreach (var capability in teacher.Capabilities)
                {
                    await connection.ExecuteAsync(new CommandDefinition(
                        InsertCapabilitySql,
                        ToCapabilityParameters(capability),
                        transaction,
                        cancellationToken: ct));
                }
            }, cancellationToken);

            return Result.Success();
        }
        catch (DbException ex) when (TryMapUniqueViolation(ex, teacher, out var error))
        {
            return Result.Failure(error);
        }
    }

    private static bool TryMapUniqueViolation(DbException exception, Teacher teacher, out DomainError error)
    {
        if (!SqlPersistenceHelper.IsUniqueConstraintViolation(exception))
        {
            error = default!;
            return false;
        }

        if (SqlPersistenceHelper.MentionsConstraint(exception, "UQ_teachers_nip"))
        {
            error = TchErrors.DuplicateNip(teacher.Nip ?? string.Empty);
            return true;
        }

        if (SqlPersistenceHelper.MentionsConstraint(exception, "UQ_teachers_nik"))
        {
            error = TchErrors.DuplicateNik(teacher.Nik ?? string.Empty);
            return true;
        }

        if (SqlPersistenceHelper.MentionsConstraint(exception, "UQ_teaching_capabilities_teacher_subject"))
        {
            error = TchErrors.DuplicateCapability(teacher.Id, Guid.Empty);
            return true;
        }

        error = default!;
        return false;
    }

    private static object ToTeacherParameters(Teacher teacher) => new
    {
        teacher.Id,
        teacher.FullName,
        teacher.Nickname,
        teacher.Nip,
        teacher.Nik,
        DateOfBirth = teacher.DateOfBirth.ToDateTime(TimeOnly.MinValue),
        Gender = teacher.Gender.ToString().ToUpperInvariant(),
        teacher.Phone,
        teacher.Email,
        teacher.Address,
        Status = teacher.Status.ToString().ToUpperInvariant(),
        teacher.CreatedAt,
        teacher.UpdatedAt
    };

    private static object ToCapabilityParameters(TeachingCapability capability) => new
    {
        capability.Id,
        capability.TeacherId,
        capability.SubjectId,
        capability.SubjectName,
        capability.CreatedAt,
        capability.UpdatedAt
    };

    private sealed record TeacherRow(
        Guid id,
        string full_name,
        string? nickname,
        string? nip,
        string? nik,
        DateTime date_of_birth,
        string gender,
        string? phone,
        string? email,
        string? address,
        string status,
        DateTime created_at,
        DateTime updated_at);

    private sealed record TeachingCapabilityRow(
        Guid id,
        Guid teacher_id,
        Guid subject_id,
        string subject_name,
        DateTime created_at,
        DateTime updated_at);
}
