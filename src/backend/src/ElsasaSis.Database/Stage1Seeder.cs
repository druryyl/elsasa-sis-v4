using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

namespace ElsasaSis.Database;

/// <summary>
/// Idempotently seeds Stage 1 verification role accounts, teacher profile, active academic year, and study groups.
/// Intended for on-demand re-seeding in Stage 1 integration tests after database resets without altering Stage 0 AdminSeeder behavior.
/// </summary>
public static class Stage1Seeder
{
    public const string DefaultAcademicYearName = "2026/2027";

    public const string DefaultPrincipalUsername = "principal";
    public const string DefaultPrincipalPassword = "Principal123!";
    public const string DefaultPrincipalDisplayName = "School Principal";
    public const string DefaultPrincipalRole = "PRINCIPAL";

    public const string DefaultTeacherUsername = "teacher.budi";
    public const string DefaultTeacherPassword = "Teacher123!";
    public const string DefaultTeacherDisplayName = "Budi Santoso";
    public const string DefaultTeacherRole = "TEACHER";
    public const string DefaultHomeroomTeacherRole = "HOMEROOM_TEACHER";

    public const string DefaultTeacherNip = "198501012010011001";
    public const string DefaultTeacherPhone = "081234567890";

    public static async Task SeedRoleAccountsAndFixturesAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var utcNow = DateTime.UtcNow;
        var hasher = new PasswordHasher<object>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        var (academicYearId, academicYearName) = await EnsureActiveAcademicYearAsync(
            connection,
            transaction,
            utcNow,
            cancellationToken);

        var principalUserId = await EnsureUserAsync(
            connection,
            transaction,
            hasher,
            DefaultPrincipalUsername,
            DefaultPrincipalPassword,
            DefaultPrincipalDisplayName,
            utcNow,
            cancellationToken);

        await EnsureUserRoleAsync(connection, transaction, principalUserId, DefaultPrincipalRole, utcNow, cancellationToken);

        var teacherUserId = await EnsureUserAsync(
            connection,
            transaction,
            hasher,
            DefaultTeacherUsername,
            DefaultTeacherPassword,
            DefaultTeacherDisplayName,
            utcNow,
            cancellationToken);

        await EnsureUserRoleAsync(connection, transaction, teacherUserId, DefaultTeacherRole, utcNow, cancellationToken);
        await EnsureUserRoleAsync(connection, transaction, teacherUserId, DefaultHomeroomTeacherRole, utcNow, cancellationToken);

        var budiTeacherId = await EnsureBudiTeacherAsync(connection, transaction, utcNow, cancellationToken);
        await EnsureUserActorTeacherAsync(connection, transaction, teacherUserId, budiTeacherId, utcNow, cancellationToken);

        await EnsureStudyGroupAsync(
            connection,
            transaction,
            academicYearId,
            academicYearName,
            schoolLevel: "SMP",
            grade: "7",
            name: "7A",
            homeroomTeacherId: budiTeacherId,
            homeroomTeacherName: DefaultTeacherDisplayName,
            utcNow,
            cancellationToken);

        await EnsureStudyGroupAsync(
            connection,
            transaction,
            academicYearId,
            academicYearName,
            schoolLevel: "SMP",
            grade: "7",
            name: "7B",
            homeroomTeacherId: null,
            homeroomTeacherName: null,
            utcNow,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<(Guid Id, string Name)> EnsureActiveAcademicYearAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        await using (var findActiveCmd = connection.CreateCommand())
        {
            findActiveCmd.Transaction = transaction;
            findActiveCmd.CommandText = """
                SELECT TOP (1) [id], [name]
                FROM [opr].[academic_years]
                WHERE [status] = 'ACTIVE'
                ORDER BY [created_at] ASC;
                """;

            await using var reader = await findActiveCmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return (reader.GetGuid(0), reader.GetString(1));
            }
        }

        Guid? existingYearId = null;
        string? existingYearName = null;

        await using (var findDefaultCmd = connection.CreateCommand())
        {
            findDefaultCmd.Transaction = transaction;
            findDefaultCmd.CommandText = """
                SELECT TOP (1) [id], [name]
                FROM [opr].[academic_years]
                WHERE [name] = @Name;
                """;
            findDefaultCmd.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 50) { Value = DefaultAcademicYearName });

            await using var reader = await findDefaultCmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                existingYearId = reader.GetGuid(0);
                existingYearName = reader.GetString(1);
            }
        }

        var academicYearId = existingYearId ?? DbUuidV7.NewGuid();
        var academicYearName = existingYearName ?? DefaultAcademicYearName;

        if (!existingYearId.HasValue)
        {
            await using var insertYearCmd = connection.CreateCommand();
            insertYearCmd.Transaction = transaction;
            insertYearCmd.CommandText = """
                INSERT INTO [opr].[academic_years] ([id], [name], [start_date], [end_date], [status], [created_at], [updated_at])
                VALUES (@Id, @Name, @StartDate, @EndDate, @Status, @CreatedAt, @UpdatedAt);
                """;
            insertYearCmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = academicYearId });
            insertYearCmd.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 50) { Value = academicYearName });
            insertYearCmd.Parameters.Add(new SqlParameter("@StartDate", SqlDbType.Date) { Value = new DateTime(2026, 7, 1) });
            insertYearCmd.Parameters.Add(new SqlParameter("@EndDate", SqlDbType.Date) { Value = new DateTime(2027, 6, 30) });
            insertYearCmd.Parameters.Add(new SqlParameter("@Status", SqlDbType.VarChar, 20) { Value = "ACTIVE" });
            insertYearCmd.Parameters.Add(new SqlParameter("@CreatedAt", SqlDbType.DateTime2, 3) { Value = utcNow });
            insertYearCmd.Parameters.Add(new SqlParameter("@UpdatedAt", SqlDbType.DateTime2, 3) { Value = utcNow });

            await insertYearCmd.ExecuteNonQueryAsync(cancellationToken);
        }
        else
        {
            await using var activateYearCmd = connection.CreateCommand();
            activateYearCmd.Transaction = transaction;
            activateYearCmd.CommandText = """
                UPDATE [opr].[academic_years]
                   SET [status] = 'ACTIVE',
                       [updated_at] = @UpdatedAt
                 WHERE [id] = @Id;
                """;
            activateYearCmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = academicYearId });
            activateYearCmd.Parameters.Add(new SqlParameter("@UpdatedAt", SqlDbType.DateTime2, 3) { Value = utcNow });

            await activateYearCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await EnsureDefaultPeriodsAsync(connection, transaction, academicYearId, utcNow, cancellationToken);

        return (academicYearId, academicYearName);
    }

    private static async Task EnsureDefaultPeriodsAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        Guid academicYearId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        bool hasAnyPeriods;
        await using (var checkCmd = connection.CreateCommand())
        {
            checkCmd.Transaction = transaction;
            checkCmd.CommandText = "SELECT CASE WHEN EXISTS (SELECT 1 FROM [opr].[operational_periods] WHERE [academic_year_id] = @AcademicYearId) THEN 1 ELSE 0 END;";
            checkCmd.Parameters.Add(new SqlParameter("@AcademicYearId", SqlDbType.UniqueIdentifier) { Value = academicYearId });

            hasAnyPeriods = Convert.ToInt32(await checkCmd.ExecuteScalarAsync(cancellationToken)) == 1;
        }

        bool hasActivePeriod;
        await using (var activeCheckCmd = connection.CreateCommand())
        {
            activeCheckCmd.Transaction = transaction;
            activeCheckCmd.CommandText = "SELECT CASE WHEN EXISTS (SELECT 1 FROM [opr].[operational_periods] WHERE [status] = 'ACTIVE') THEN 1 ELSE 0 END;";

            hasActivePeriod = Convert.ToInt32(await activeCheckCmd.ExecuteScalarAsync(cancellationToken)) == 1;
        }

        if (!hasAnyPeriods)
        {
            await InsertPeriodAsync(
                connection,
                transaction,
                academicYearId,
                "Semester 1",
                new DateTime(2026, 7, 1),
                new DateTime(2026, 12, 20),
                hasActivePeriod ? "INACTIVE" : "ACTIVE",
                utcNow,
                cancellationToken);

            await InsertPeriodAsync(
                connection,
                transaction,
                academicYearId,
                "Semester 2",
                new DateTime(2027, 1, 5),
                new DateTime(2027, 6, 30),
                "INACTIVE",
                utcNow,
                cancellationToken);
        }
        else if (!hasActivePeriod)
        {
            await using var activateEarliestCmd = connection.CreateCommand();
            activateEarliestCmd.Transaction = transaction;
            activateEarliestCmd.CommandText = """
                UPDATE [opr].[operational_periods]
                   SET [status] = 'ACTIVE',
                       [updated_at] = @UpdatedAt
                 WHERE [id] = (
                     SELECT TOP (1) [id]
                     FROM [opr].[operational_periods]
                     WHERE [academic_year_id] = @AcademicYearId
                     ORDER BY [start_date] ASC
                 );
                """;
            activateEarliestCmd.Parameters.Add(new SqlParameter("@AcademicYearId", SqlDbType.UniqueIdentifier) { Value = academicYearId });
            activateEarliestCmd.Parameters.Add(new SqlParameter("@UpdatedAt", SqlDbType.DateTime2, 3) { Value = utcNow });

            await activateEarliestCmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task InsertPeriodAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        Guid academicYearId,
        string name,
        DateTime startDate,
        DateTime endDate,
        string status,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = """
            INSERT INTO [opr].[operational_periods] ([id], [academic_year_id], [name], [start_date], [end_date], [status], [created_at], [updated_at])
            VALUES (@Id, @AcademicYearId, @Name, @StartDate, @EndDate, @Status, @CreatedAt, @UpdatedAt);
            """;
        cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = DbUuidV7.NewGuid() });
        cmd.Parameters.Add(new SqlParameter("@AcademicYearId", SqlDbType.UniqueIdentifier) { Value = academicYearId });
        cmd.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 50) { Value = name });
        cmd.Parameters.Add(new SqlParameter("@StartDate", SqlDbType.Date) { Value = startDate });
        cmd.Parameters.Add(new SqlParameter("@EndDate", SqlDbType.Date) { Value = endDate });
        cmd.Parameters.Add(new SqlParameter("@Status", SqlDbType.VarChar, 20) { Value = status });
        cmd.Parameters.Add(new SqlParameter("@CreatedAt", SqlDbType.DateTime2, 3) { Value = utcNow });
        cmd.Parameters.Add(new SqlParameter("@UpdatedAt", SqlDbType.DateTime2, 3) { Value = utcNow });

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<Guid> EnsureUserAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        PasswordHasher<object> hasher,
        string username,
        string plainPassword,
        string displayName,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        await using (var findCmd = connection.CreateCommand())
        {
            findCmd.Transaction = transaction;
            findCmd.CommandText = "SELECT [id] FROM [auth].[users] WHERE [username] = @Username;";
            findCmd.Parameters.Add(new SqlParameter("@Username", SqlDbType.VarChar, 50) { Value = username });

            var scalar = await findCmd.ExecuteScalarAsync(cancellationToken);
            if (scalar is Guid existingId)
            {
                return existingId;
            }
        }

        var userId = DbUuidV7.NewGuid();
        var passwordHash = hasher.HashPassword(new object(), plainPassword);

        await using var insertCmd = connection.CreateCommand();
        insertCmd.Transaction = transaction;
        insertCmd.CommandText = """
            INSERT INTO [auth].[users] ([id], [username], [password_hash], [email], [display_name], [is_active], [created_at], [updated_at])
            VALUES (@Id, @Username, @PasswordHash, @Email, @DisplayName, @IsActive, @CreatedAt, @UpdatedAt);
            """;
        insertCmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = userId });
        insertCmd.Parameters.Add(new SqlParameter("@Username", SqlDbType.VarChar, 50) { Value = username });
        insertCmd.Parameters.Add(new SqlParameter("@PasswordHash", SqlDbType.VarChar, 255) { Value = passwordHash });
        insertCmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.VarChar, 100) { Value = DBNull.Value });
        insertCmd.Parameters.Add(new SqlParameter("@DisplayName", SqlDbType.NVarChar, 100) { Value = displayName });
        insertCmd.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = true });
        insertCmd.Parameters.Add(new SqlParameter("@CreatedAt", SqlDbType.DateTime2, 3) { Value = utcNow });
        insertCmd.Parameters.Add(new SqlParameter("@UpdatedAt", SqlDbType.DateTime2, 3) { Value = utcNow });

        await insertCmd.ExecuteNonQueryAsync(cancellationToken);
        return userId;
    }

    private static async Task EnsureUserRoleAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        Guid userId,
        string roleCode,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = """
            IF NOT EXISTS (SELECT 1 FROM [auth].[user_roles] WHERE [user_id] = @UserId AND [role_code] = @RoleCode)
            BEGIN
                INSERT INTO [auth].[user_roles] ([user_id], [role_code], [created_at], [updated_at])
                VALUES (@UserId, @RoleCode, @CreatedAt, @UpdatedAt);
            END
            """;
        cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.UniqueIdentifier) { Value = userId });
        cmd.Parameters.Add(new SqlParameter("@RoleCode", SqlDbType.VarChar, 30) { Value = roleCode });
        cmd.Parameters.Add(new SqlParameter("@CreatedAt", SqlDbType.DateTime2, 3) { Value = utcNow });
        cmd.Parameters.Add(new SqlParameter("@UpdatedAt", SqlDbType.DateTime2, 3) { Value = utcNow });

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<Guid> EnsureBudiTeacherAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        await using (var findCmd = connection.CreateCommand())
        {
            findCmd.Transaction = transaction;
            findCmd.CommandText = "SELECT [id] FROM [tch].[teachers] WHERE [nip] = @Nip;";
            findCmd.Parameters.Add(new SqlParameter("@Nip", SqlDbType.NVarChar, 30) { Value = DefaultTeacherNip });

            var scalar = await findCmd.ExecuteScalarAsync(cancellationToken);
            if (scalar is Guid existingId)
            {
                return existingId;
            }
        }

        var teacherId = DbUuidV7.NewGuid();

        await using var insertCmd = connection.CreateCommand();
        insertCmd.Transaction = transaction;
        insertCmd.CommandText = """
            INSERT INTO [tch].[teachers] (
                [id], [full_name], [nickname], [nip], [nik], [date_of_birth],
                [gender], [phone], [email], [address], [status], [created_at], [updated_at]
            )
            VALUES (
                @Id, @FullName, @Nickname, @Nip, @Nik, @DateOfBirth,
                @Gender, @Phone, @Email, @Address, @Status, @CreatedAt, @UpdatedAt
            );
            """;
        insertCmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = teacherId });
        insertCmd.Parameters.Add(new SqlParameter("@FullName", SqlDbType.NVarChar, 150) { Value = DefaultTeacherDisplayName });
        insertCmd.Parameters.Add(new SqlParameter("@Nickname", SqlDbType.NVarChar, 50) { Value = "Budi" });
        insertCmd.Parameters.Add(new SqlParameter("@Nip", SqlDbType.NVarChar, 30) { Value = DefaultTeacherNip });
        insertCmd.Parameters.Add(new SqlParameter("@Nik", SqlDbType.NVarChar, 30) { Value = DBNull.Value });
        insertCmd.Parameters.Add(new SqlParameter("@DateOfBirth", SqlDbType.Date) { Value = new DateTime(1985, 1, 1) });
        insertCmd.Parameters.Add(new SqlParameter("@Gender", SqlDbType.VarChar, 10) { Value = "MALE" });
        insertCmd.Parameters.Add(new SqlParameter("@Phone", SqlDbType.NVarChar, 30) { Value = DefaultTeacherPhone });
        insertCmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 150) { Value = DBNull.Value });
        insertCmd.Parameters.Add(new SqlParameter("@Address", SqlDbType.NVarChar, 500) { Value = DBNull.Value });
        insertCmd.Parameters.Add(new SqlParameter("@Status", SqlDbType.VarChar, 20) { Value = "ACTIVE" });
        insertCmd.Parameters.Add(new SqlParameter("@CreatedAt", SqlDbType.DateTime2, 3) { Value = utcNow });
        insertCmd.Parameters.Add(new SqlParameter("@UpdatedAt", SqlDbType.DateTime2, 3) { Value = utcNow });

        await insertCmd.ExecuteNonQueryAsync(cancellationToken);
        return teacherId;
    }

    private static async Task EnsureUserActorTeacherAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        Guid userId,
        Guid teacherId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = """
            IF NOT EXISTS (SELECT 1 FROM [auth].[user_actors] WHERE [user_id] = @UserId)
            BEGIN
                INSERT INTO [auth].[user_actors] ([user_id], [teacher_id], [student_id], [guardian_id], [created_at], [updated_at])
                VALUES (@UserId, @TeacherId, NULL, NULL, @CreatedAt, @UpdatedAt);
            END
            ELSE
            BEGIN
                UPDATE [auth].[user_actors]
                   SET [teacher_id] = @TeacherId,
                       [updated_at] = @UpdatedAt
                 WHERE [user_id] = @UserId
                   AND [teacher_id] IS NULL;
            END
            """;
        cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.UniqueIdentifier) { Value = userId });
        cmd.Parameters.Add(new SqlParameter("@TeacherId", SqlDbType.UniqueIdentifier) { Value = teacherId });
        cmd.Parameters.Add(new SqlParameter("@CreatedAt", SqlDbType.DateTime2, 3) { Value = utcNow });
        cmd.Parameters.Add(new SqlParameter("@UpdatedAt", SqlDbType.DateTime2, 3) { Value = utcNow });

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task EnsureStudyGroupAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        Guid academicYearId,
        string academicYearName,
        string schoolLevel,
        string grade,
        string name,
        Guid? homeroomTeacherId,
        string? homeroomTeacherName,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = """
            IF NOT EXISTS (SELECT 1 FROM [std].[study_groups] WHERE [academic_year_id] = @AcademicYearId AND [name] = @Name)
            BEGIN
                INSERT INTO [std].[study_groups] (
                    [id], [academic_year_id], [academic_year_name], [school_level], [grade],
                    [name], [homeroom_teacher_id], [homeroom_teacher_name], [created_at], [updated_at]
                )
                VALUES (
                    @Id, @AcademicYearId, @AcademicYearName, @SchoolLevel, @Grade,
                    @Name, @HomeroomTeacherId, @HomeroomTeacherName, @CreatedAt, @UpdatedAt
                );
            END
            """;
        cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = DbUuidV7.NewGuid() });
        cmd.Parameters.Add(new SqlParameter("@AcademicYearId", SqlDbType.UniqueIdentifier) { Value = academicYearId });
        cmd.Parameters.Add(new SqlParameter("@AcademicYearName", SqlDbType.NVarChar, 50) { Value = academicYearName });
        cmd.Parameters.Add(new SqlParameter("@SchoolLevel", SqlDbType.VarChar, 10) { Value = schoolLevel });
        cmd.Parameters.Add(new SqlParameter("@Grade", SqlDbType.VarChar, 20) { Value = grade });
        cmd.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 50) { Value = name });
        cmd.Parameters.Add(new SqlParameter("@HomeroomTeacherId", SqlDbType.UniqueIdentifier) { Value = (object?)homeroomTeacherId ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@HomeroomTeacherName", SqlDbType.NVarChar, 150) { Value = (object?)homeroomTeacherName ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@CreatedAt", SqlDbType.DateTime2, 3) { Value = utcNow });
        cmd.Parameters.Add(new SqlParameter("@UpdatedAt", SqlDbType.DateTime2, 3) { Value = utcNow });

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
