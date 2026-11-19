using System.Data.Common;
using Dapper;
using ElsasaSis.Modules.Std.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Std.Infrastructure.Persistence;

public sealed class StudentPlacementRepository(ISqlConnectionFactory connectionFactory) : IStudentPlacementRepository
{
    private const string SelectByIdSql = """
        SELECT id, student_id, academic_year_id, academic_year_name,
               school_level, grade, study_group_id, study_group_name,
               created_at, updated_at
        FROM std.student_placements
        WHERE id = @Id;
        """;

    private const string SelectByStudentAndYearSql = """
        SELECT id, student_id, academic_year_id, academic_year_name,
               school_level, grade, study_group_id, study_group_name,
               created_at, updated_at
        FROM std.student_placements
        WHERE student_id = @StudentId
          AND academic_year_id = @AcademicYearId;
        """;

    private const string UpsertPlacementSql = """
        UPDATE std.student_placements WITH (UPDLOCK, HOLDLOCK)
        SET academic_year_name = @AcademicYearName,
            school_level = @SchoolLevel,
            grade = @Grade,
            study_group_id = @StudyGroupId,
            study_group_name = @StudyGroupName,
            updated_at = @UpdatedAt
        WHERE student_id = @StudentId
          AND academic_year_id = @AcademicYearId;

        IF @@ROWCOUNT = 0
        BEGIN
            INSERT INTO std.student_placements (
                id, student_id, academic_year_id, academic_year_name,
                school_level, grade, study_group_id, study_group_name,
                created_at, updated_at
            )
            VALUES (
                @Id, @StudentId, @AcademicYearId, @AcademicYearName,
                @SchoolLevel, @Grade, @StudyGroupId, @StudyGroupName,
                @CreatedAt, @UpdatedAt
            );
        END;
        """;

    public async Task<StudentPlacement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<StudentPlacementRow>(
            new CommandDefinition(SelectByIdSql, new { Id = id }, cancellationToken: cancellationToken));

        return MapRow(row);
    }

    public async Task<StudentPlacement?> GetByStudentAndAcademicYearAsync(
        Guid studentId,
        Guid academicYearId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<StudentPlacementRow>(new CommandDefinition(
            SelectByStudentAndYearSql,
            new { StudentId = studentId, AcademicYearId = academicYearId },
            cancellationToken: cancellationToken));

        return MapRow(row);
    }

    public Task<Result> UpsertAsync(StudentPlacement placement, CancellationToken cancellationToken = default) =>
        SqlPersistenceHelper.ExecuteResultInTransactionAsync(
            connectionFactory,
            async (connection, transaction, ct) =>
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    UpsertPlacementSql,
                    new
                    {
                        placement.Id,
                        placement.StudentId,
                        placement.AcademicYearId,
                        placement.AcademicYearName,
                        SchoolLevel = placement.SchoolLevel.ToCode(),
                        placement.Grade,
                        placement.StudyGroupId,
                        placement.StudyGroupName,
                        placement.CreatedAt,
                        placement.UpdatedAt
                    },
                    transaction,
                    cancellationToken: ct));

                return Result.Success();
            },
            ex => MapUniqueConstraint(ex, placement),
            cancellationToken);

    private static Result? MapUniqueConstraint(DbException ex, StudentPlacement placement)
    {
        if (ex.Message.Contains("UQ_student_placements_student_year", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(StdErrors.PlacementAlreadyExistsForAcademicYear(
                placement.StudentId,
                placement.AcademicYearId));
        }

        return null;
    }

    private static StudentPlacement? MapRow(StudentPlacementRow? row) =>
        row is null
            ? null
            : StudentPlacement.Rehydrate(
                row.id,
                row.student_id,
                row.academic_year_id,
                row.academic_year_name,
                Enum.Parse<SchoolLevel>(row.school_level, ignoreCase: true),
                row.grade,
                row.study_group_id,
                row.study_group_name,
                SqlPersistenceHelper.SpecifyUtc(row.created_at),
                SqlPersistenceHelper.SpecifyUtc(row.updated_at));

    private sealed class StudentPlacementRow
    {
        public Guid id { get; set; }
        public Guid student_id { get; set; }
        public Guid academic_year_id { get; set; }
        public string academic_year_name { get; set; } = string.Empty;
        public string school_level { get; set; } = string.Empty;
        public string grade { get; set; } = string.Empty;
        public Guid? study_group_id { get; set; }
        public string? study_group_name { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}
