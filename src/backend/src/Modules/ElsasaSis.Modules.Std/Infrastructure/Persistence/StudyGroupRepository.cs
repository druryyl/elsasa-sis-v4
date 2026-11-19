using System.Data.Common;
using Dapper;
using ElsasaSis.Modules.Std.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Std.Infrastructure.Persistence;

public sealed class StudyGroupRepository(ISqlConnectionFactory connectionFactory) : IStudyGroupRepository
{
    private const string SelectByIdSql = """
        SELECT id, academic_year_id, academic_year_name, school_level, grade, name,
               homeroom_teacher_id, homeroom_teacher_name, created_at, updated_at
        FROM std.study_groups
        WHERE id = @Id;
        """;

    private const string ExistsByYearAndNameSql = """
        SELECT CASE WHEN EXISTS (
            SELECT 1
            FROM std.study_groups
            WHERE academic_year_id = @AcademicYearId
              AND LOWER(LTRIM(RTRIM(name))) = LOWER(LTRIM(RTRIM(@Name)))
              AND (@ExcludeId IS NULL OR id <> @ExcludeId)
        ) THEN 1 ELSE 0 END;
        """;

    private const string ExistsByYearAndNameWithLockSql = """
        SELECT CASE WHEN EXISTS (
            SELECT 1
            FROM std.study_groups WITH (UPDLOCK, HOLDLOCK)
            WHERE academic_year_id = @AcademicYearId
              AND LOWER(LTRIM(RTRIM(name))) = LOWER(LTRIM(RTRIM(@Name)))
              AND (@ExcludeId IS NULL OR id <> @ExcludeId)
        ) THEN 1 ELSE 0 END;
        """;

    private const string InsertStudyGroupSql = """
        INSERT INTO std.study_groups (
            id, academic_year_id, academic_year_name, school_level, grade, name,
            homeroom_teacher_id, homeroom_teacher_name, created_at, updated_at
        )
        VALUES (
            @Id, @AcademicYearId, @AcademicYearName, @SchoolLevel, @Grade, @Name,
            @HomeroomTeacherId, @HomeroomTeacherName, @CreatedAt, @UpdatedAt
        );
        """;

    private const string UpdateStudyGroupSql = """
        UPDATE std.study_groups
        SET academic_year_id = @AcademicYearId,
            academic_year_name = @AcademicYearName,
            school_level = @SchoolLevel,
            grade = @Grade,
            name = @Name,
            homeroom_teacher_id = @HomeroomTeacherId,
            homeroom_teacher_name = @HomeroomTeacherName,
            updated_at = @UpdatedAt
        WHERE id = @Id;
        """;

    private const string PropagateStudyGroupNameToPlacementsSql = """
        UPDATE std.student_placements
        SET study_group_name = @Name,
            updated_at = @UpdatedAt
        WHERE study_group_id = @Id
          AND (study_group_name IS NULL OR study_group_name <> @Name);
        """;

    public async Task<StudyGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<StudyGroupRow>(
            new CommandDefinition(SelectByIdSql, new { Id = id }, cancellationToken: cancellationToken));

        return row is null
            ? null
            : StudyGroup.Rehydrate(
                row.id,
                row.academic_year_id,
                row.academic_year_name,
                Enum.Parse<SchoolLevel>(row.school_level, ignoreCase: true),
                row.grade,
                row.name,
                row.homeroom_teacher_id,
                row.homeroom_teacher_name,
                SqlPersistenceHelper.SpecifyUtc(row.created_at),
                SqlPersistenceHelper.SpecifyUtc(row.updated_at));
    }

    public async Task<bool> ExistsByYearAndNameAsync(
        Guid academicYearId,
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            ExistsByYearAndNameSql,
            new { AcademicYearId = academicYearId, Name = name, ExcludeId = excludeId },
            cancellationToken: cancellationToken));
    }

    public Task<Result> AddAsync(StudyGroup studyGroup, CancellationToken cancellationToken = default) =>
        SqlPersistenceHelper.ExecuteResultInTransactionAsync(
            connectionFactory,
            async (connection, transaction, ct) =>
            {
                var exists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                    ExistsByYearAndNameWithLockSql,
                    new { studyGroup.AcademicYearId, studyGroup.Name, ExcludeId = (Guid?)null },
                    transaction,
                    cancellationToken: ct));

                if (exists)
                {
                    return Result.Failure(StdErrors.DuplicateStudyGroupName(studyGroup.AcademicYearId, studyGroup.Name));
                }

                await connection.ExecuteAsync(new CommandDefinition(
                    InsertStudyGroupSql,
                    ToParameters(studyGroup),
                    transaction,
                    cancellationToken: ct));

                return Result.Success();
            },
            ex => MapUniqueConstraint(ex, studyGroup),
            cancellationToken);

    public Task<Result> UpdateAsync(StudyGroup studyGroup, CancellationToken cancellationToken = default) =>
        SqlPersistenceHelper.ExecuteResultInTransactionAsync(
            connectionFactory,
            async (connection, transaction, ct) =>
            {
                var exists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                    ExistsByYearAndNameWithLockSql,
                    new { studyGroup.AcademicYearId, studyGroup.Name, ExcludeId = (Guid?)studyGroup.Id },
                    transaction,
                    cancellationToken: ct));

                if (exists)
                {
                    return Result.Failure(StdErrors.DuplicateStudyGroupName(studyGroup.AcademicYearId, studyGroup.Name));
                }

                await connection.ExecuteAsync(new CommandDefinition(
                    UpdateStudyGroupSql,
                    ToParameters(studyGroup),
                    transaction,
                    cancellationToken: ct));

                // EXT-STU-002: Propagate renamed study_group_name snapshot to student_placements.
                await connection.ExecuteAsync(new CommandDefinition(
                    PropagateStudyGroupNameToPlacementsSql,
                    new { studyGroup.Id, studyGroup.Name, studyGroup.UpdatedAt },
                    transaction,
                    cancellationToken: ct));

                return Result.Success();
            },
            ex => MapUniqueConstraint(ex, studyGroup),
            cancellationToken);

    private static Result? MapUniqueConstraint(DbException ex, StudyGroup studyGroup)
    {
        if (ex.Message.Contains("UQ_study_groups_year_name", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(StdErrors.DuplicateStudyGroupName(studyGroup.AcademicYearId, studyGroup.Name));
        }

        return null;
    }

    private static object ToParameters(StudyGroup studyGroup) => new
    {
        studyGroup.Id,
        studyGroup.AcademicYearId,
        studyGroup.AcademicYearName,
        SchoolLevel = studyGroup.SchoolLevel.ToCode(),
        studyGroup.Grade,
        studyGroup.Name,
        studyGroup.HomeroomTeacherId,
        studyGroup.HomeroomTeacherName,
        studyGroup.CreatedAt,
        studyGroup.UpdatedAt
    };

    private sealed class StudyGroupRow
    {
        public Guid id { get; set; }
        public Guid academic_year_id { get; set; }
        public string academic_year_name { get; set; } = string.Empty;
        public string school_level { get; set; } = string.Empty;
        public string grade { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public Guid? homeroom_teacher_id { get; set; }
        public string? homeroom_teacher_name { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}
