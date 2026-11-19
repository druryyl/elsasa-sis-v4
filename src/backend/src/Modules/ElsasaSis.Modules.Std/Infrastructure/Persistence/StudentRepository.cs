using Dapper;
using ElsasaSis.Modules.Std.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Std.Infrastructure.Persistence;

public sealed class StudentRepository(ISqlConnectionFactory connectionFactory) : IStudentRepository
{
    private const string SelectStudentSql = """
        SELECT id, candidate_id, nis, nisn, full_name, nickname, date_of_birth, gender,
               phone, email, address, enrollment_date, status, created_at, updated_at
        FROM std.students
        WHERE id = @Id;
        """;

    private const string SelectStudentGuardiansSql = """
        SELECT id, student_id, full_name, relationship_type, phone, email, address, created_at, updated_at
        FROM std.student_guardians
        WHERE student_id = @StudentId
        ORDER BY relationship_type ASC, full_name ASC, id ASC;
        """;

    private const string ExistsByNisSql = """
        SELECT CASE WHEN EXISTS (
            SELECT 1
            FROM std.students
            WHERE nis = @Nis
              AND (@ExcludeStudentId IS NULL OR id <> @ExcludeStudentId)
        ) THEN 1 ELSE 0 END;
        """;

    private const string ExistsByNisnSql = """
        SELECT CASE WHEN EXISTS (
            SELECT 1
            FROM std.students
            WHERE nisn = @Nisn
              AND (@ExcludeStudentId IS NULL OR id <> @ExcludeStudentId)
        ) THEN 1 ELSE 0 END;
        """;

    private const string UpdateStudentSql = """
        UPDATE std.students
        SET nis = @Nis,
            nisn = @Nisn,
            full_name = @FullName,
            nickname = @Nickname,
            date_of_birth = @DateOfBirth,
            gender = @Gender,
            phone = @Phone,
            email = @Email,
            address = @Address,
            enrollment_date = @EnrollmentDate,
            status = @Status,
            updated_at = @UpdatedAt
        WHERE id = @Id;
        """;

    private const string DeleteStudentGuardiansSql = """
        DELETE FROM std.student_guardians
        WHERE student_id = @StudentId;
        """;

    public async Task<Student?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<StudentRow>(
            new CommandDefinition(SelectStudentSql, new { Id = id }, cancellationToken: cancellationToken));

        if (row is null)
        {
            return null;
        }

        var guardianRows = await connection.QueryAsync<StudentGuardianRow>(
            new CommandDefinition(SelectStudentGuardiansSql, new { StudentId = id }, cancellationToken: cancellationToken));

        var guardians = guardianRows.Select(g => StudentGuardian.Rehydrate(
            g.id,
            g.student_id,
            g.full_name,
            Enum.Parse<GuardianRelationshipType>(g.relationship_type, ignoreCase: true),
            g.phone,
            g.email,
            g.address,
            SqlPersistenceHelper.SpecifyUtc(g.created_at),
            SqlPersistenceHelper.SpecifyUtc(g.updated_at)));

        return Student.Rehydrate(
            row.id,
            row.candidate_id,
            row.nis,
            row.nisn,
            row.full_name,
            row.nickname,
            DateOnly.FromDateTime(row.date_of_birth),
            Enum.Parse<Gender>(row.gender, ignoreCase: true),
            row.phone,
            row.email,
            row.address,
            DateOnly.FromDateTime(row.enrollment_date),
            Enum.Parse<StudentStatus>(row.status, ignoreCase: true),
            SqlPersistenceHelper.SpecifyUtc(row.created_at),
            SqlPersistenceHelper.SpecifyUtc(row.updated_at),
            guardians);
    }

    public async Task<bool> ExistsByNisAsync(string nis, Guid? excludeStudentId = null, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            ExistsByNisSql,
            new { Nis = nis.Trim(), ExcludeStudentId = excludeStudentId },
            cancellationToken: cancellationToken));
    }

    public async Task<bool> ExistsByNisnAsync(string nisn, Guid? excludeStudentId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nisn))
        {
            return false;
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            ExistsByNisnSql,
            new { Nisn = nisn.Trim(), ExcludeStudentId = excludeStudentId },
            cancellationToken: cancellationToken));
    }

    public Task<Result> AddAsync(Student student, CancellationToken cancellationToken = default) =>
        SqlPersistenceHelper.ExecuteResultInTransactionAsync(
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

                await connection.ExecuteAsync(new CommandDefinition(
                    StudentPersistenceShared.InsertStudentSql,
                    StudentPersistenceShared.ToStudentParameters(student),
                    transaction,
                    cancellationToken: ct));

                foreach (var guardian in student.Guardians)
                {
                    await connection.ExecuteAsync(new CommandDefinition(
                        StudentPersistenceShared.InsertStudentGuardianSql,
                        StudentPersistenceShared.ToStudentGuardianParameters(guardian),
                        transaction,
                        cancellationToken: ct));
                }

                return Result.Success();
            },
            ex =>
            {
                var error = StudentPersistenceShared.MapStudentUniqueConstraintError(ex, student.Nis, student.Nisn);
                return error is null ? null : Result.Failure(error);
            },
            cancellationToken);

    public Task<Result> UpdateAsync(Student student, CancellationToken cancellationToken = default) =>
        SqlPersistenceHelper.ExecuteResultInTransactionAsync(
            connectionFactory,
            async (connection, transaction, ct) =>
            {
                var nisExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                    StudentPersistenceShared.ExistsOtherNisWithLockSql,
                    new { student.Nis, ExcludeStudentId = (Guid?)student.Id },
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
                        new { student.Nisn, ExcludeStudentId = (Guid?)student.Id },
                        transaction,
                        cancellationToken: ct));

                    if (nisnExists)
                    {
                        return Result.Failure(StdErrors.DuplicateNisn(student.Nisn));
                    }
                }

                await connection.ExecuteAsync(new CommandDefinition(
                    UpdateStudentSql,
                    StudentPersistenceShared.ToStudentParameters(student),
                    transaction,
                    cancellationToken: ct));

                await connection.ExecuteAsync(new CommandDefinition(
                    DeleteStudentGuardiansSql,
                    new { StudentId = student.Id },
                    transaction,
                    cancellationToken: ct));

                foreach (var guardian in student.Guardians)
                {
                    await connection.ExecuteAsync(new CommandDefinition(
                        StudentPersistenceShared.InsertStudentGuardianSql,
                        StudentPersistenceShared.ToStudentGuardianParameters(guardian),
                        transaction,
                        cancellationToken: ct));
                }

                return Result.Success();
            },
            ex =>
            {
                var error = StudentPersistenceShared.MapStudentUniqueConstraintError(ex, student.Nis, student.Nisn);
                return error is null ? null : Result.Failure(error);
            },
            cancellationToken);

    private sealed class StudentRow
    {
        public Guid id { get; set; }
        public Guid? candidate_id { get; set; }
        public string nis { get; set; } = string.Empty;
        public string? nisn { get; set; }
        public string full_name { get; set; } = string.Empty;
        public string? nickname { get; set; }
        public DateTime date_of_birth { get; set; }
        public string gender { get; set; } = string.Empty;
        public string? phone { get; set; }
        public string? email { get; set; }
        public string? address { get; set; }
        public DateTime enrollment_date { get; set; }
        public string status { get; set; } = string.Empty;
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    private sealed class StudentGuardianRow
    {
        public Guid id { get; set; }
        public Guid student_id { get; set; }
        public string full_name { get; set; } = string.Empty;
        public string relationship_type { get; set; } = string.Empty;
        public string? phone { get; set; }
        public string? email { get; set; }
        public string? address { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}
