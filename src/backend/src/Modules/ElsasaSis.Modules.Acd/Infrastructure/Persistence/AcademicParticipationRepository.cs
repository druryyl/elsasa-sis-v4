using System.Data.Common;
using Dapper;
using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.Modules.Acd.Domain.Model;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Acd.Infrastructure.Persistence;

public sealed class AcademicParticipationRepository(ISqlConnectionFactory connectionFactory) : IAcademicParticipationRepository
{
    private const string SelectColumns = """
        id, student_id, student_name, student_nis, subject_stage_id, subject_stage_name,
        subject_id, subject_name, operational_period_id, operational_period_name,
        academic_year_id, academic_year_name, study_group_id, study_group_name,
        status, created_at, updated_at
        """;

    private const string SelectByIdSql = $"""
        SELECT {SelectColumns}
        FROM acd.academic_participations
        WHERE id = @Id;
        """;

    private const string SelectByStudentStagePeriodSql = $"""
        SELECT {SelectColumns}
        FROM acd.academic_participations
        WHERE student_id = @StudentId
          AND subject_stage_id = @SubjectStageId
          AND operational_period_id = @OperationalPeriodId;
        """;

    private const string SelectByCriteriaSql = $"""
        SELECT {SelectColumns}
        FROM acd.academic_participations
        WHERE (@SubjectStageId IS NULL OR subject_stage_id = @SubjectStageId)
          AND (@StudyGroupId IS NULL OR study_group_id = @StudyGroupId)
          AND (@OperationalPeriodId IS NULL OR operational_period_id = @OperationalPeriodId)
        ORDER BY student_name ASC;
        """;

    private const string SelectEnrolledStudentIdsSql = """
        SELECT student_id
        FROM acd.academic_participations
        WHERE subject_stage_id = @SubjectStageId
          AND operational_period_id = @OperationalPeriodId
          AND student_id IN @StudentIds;
        """;

    private const string SelectActivePlacementsByStudyGroupSql = """
        SELECT
            sp.student_id AS StudentId,
            COALESCE(s.full_name, 'Unknown') AS StudentName,
            COALESCE(s.nis, '') AS StudentNis,
            sp.study_group_id AS StudyGroupId,
            COALESCE(sg.name, sp.study_group_name) AS StudyGroupName,
            sp.academic_year_id AS AcademicYearId,
            COALESCE(sg.academic_year_name, sp.academic_year_name) AS AcademicYearName
        FROM std.student_placements sp
        INNER JOIN std.students s ON s.id = sp.student_id
        LEFT JOIN std.study_groups sg ON sg.id = sp.study_group_id
        WHERE sp.study_group_id = @StudyGroupId
          AND sp.academic_year_id = @AcademicYearId
          AND s.status = 'ACTIVE'
        ORDER BY s.full_name ASC;
        """;

    private const string InsertSql = """
        INSERT INTO acd.academic_participations (
            id, student_id, student_name, student_nis,
            subject_stage_id, subject_stage_name, subject_id, subject_name,
            operational_period_id, operational_period_name,
            academic_year_id, academic_year_name,
            study_group_id, study_group_name,
            status, created_at, updated_at
        )
        VALUES (
            @Id, @StudentId, @StudentName, @StudentNis,
            @SubjectStageId, @SubjectStageName, @SubjectId, @SubjectName,
            @OperationalPeriodId, @OperationalPeriodName,
            @AcademicYearId, @AcademicYearName,
            @StudyGroupId, @StudyGroupName,
            @Status, @CreatedAt, @UpdatedAt
        );
        """;

    private const string InsertIfNotExistsSql = """
        IF NOT EXISTS (
            SELECT 1 FROM acd.academic_participations
            WHERE student_id = @StudentId
              AND subject_stage_id = @SubjectStageId
              AND operational_period_id = @OperationalPeriodId
        )
        BEGIN
            INSERT INTO acd.academic_participations (
                id, student_id, student_name, student_nis,
                subject_stage_id, subject_stage_name, subject_id, subject_name,
                operational_period_id, operational_period_name,
                academic_year_id, academic_year_name,
                study_group_id, study_group_name,
                status, created_at, updated_at
            )
            VALUES (
                @Id, @StudentId, @StudentName, @StudentNis,
                @SubjectStageId, @SubjectStageName, @SubjectId, @SubjectName,
                @OperationalPeriodId, @OperationalPeriodName,
                @AcademicYearId, @AcademicYearName,
                @StudyGroupId, @StudyGroupName,
                @Status, @CreatedAt, @UpdatedAt
            );
        END;
        """;

    private const string UpdateSql = """
        UPDATE acd.academic_participations
        SET status = @Status,
            study_group_id = @StudyGroupId,
            study_group_name = @StudyGroupName,
            updated_at = @UpdatedAt
        WHERE id = @Id;
        """;

    public async Task<AcademicParticipation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<ParticipationRow>(
            new CommandDefinition(SelectByIdSql, new { Id = id }, cancellationToken: cancellationToken));

        return row is null ? null : MapToDomain(row);
    }

    public async Task<AcademicParticipation?> GetByStudentStagePeriodAsync(
        Guid studentId,
        Guid subjectStageId,
        Guid operationalPeriodId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<ParticipationRow>(
            new CommandDefinition(
                SelectByStudentStagePeriodSql,
                new
                {
                    StudentId = studentId,
                    SubjectStageId = subjectStageId,
                    OperationalPeriodId = operationalPeriodId
                },
                cancellationToken: cancellationToken));

        return row is null ? null : MapToDomain(row);
    }

    public async Task<IReadOnlyList<AcademicParticipation>> GetByCriteriaAsync(
        Guid? subjectStageId,
        Guid? studyGroupId,
        Guid? operationalPeriodId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<ParticipationRow>(
            new CommandDefinition(
                SelectByCriteriaSql,
                new
                {
                    SubjectStageId = subjectStageId,
                    StudyGroupId = studyGroupId,
                    OperationalPeriodId = operationalPeriodId
                },
                cancellationToken: cancellationToken));

        return rows.Select(MapToDomain).ToList();
    }

    public async Task<IReadOnlySet<Guid>> GetEnrolledStudentIdsAsync(
        Guid subjectStageId,
        Guid operationalPeriodId,
        IEnumerable<Guid> studentIds,
        CancellationToken cancellationToken = default)
    {
        var idList = studentIds.ToList();
        if (idList.Count == 0)
        {
            return new HashSet<Guid>();
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var enrolledIds = await connection.QueryAsync<Guid>(
            new CommandDefinition(
                SelectEnrolledStudentIdsSql,
                new
                {
                    SubjectStageId = subjectStageId,
                    OperationalPeriodId = operationalPeriodId,
                    StudentIds = idList
                },
                cancellationToken: cancellationToken));

        return enrolledIds.ToHashSet();
    }

    public async Task<IReadOnlyList<StudentPlacementRosterDto>> GetActivePlacementsByStudyGroupAsync(
        Guid studyGroupId,
        Guid academicYearId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<StudentPlacementRosterDto>(
            new CommandDefinition(
                SelectActivePlacementsByStudyGroupSql,
                new
                {
                    StudyGroupId = studyGroupId,
                    AcademicYearId = academicYearId
                },
                cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task<Result> AddAsync(AcademicParticipation participation, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            await connection.ExecuteAsync(
                new CommandDefinition(
                    InsertSql,
                    new
                    {
                        participation.Id,
                        participation.StudentId,
                        participation.StudentName,
                        participation.StudentNis,
                        participation.SubjectStageId,
                        participation.SubjectStageName,
                        participation.SubjectId,
                        participation.SubjectName,
                        participation.OperationalPeriodId,
                        participation.OperationalPeriodName,
                        participation.AcademicYearId,
                        participation.AcademicYearName,
                        participation.StudyGroupId,
                        participation.StudyGroupName,
                        Status = participation.Status.ToCode(),
                        participation.CreatedAt,
                        participation.UpdatedAt
                    },
                    cancellationToken: cancellationToken));

            return Result.Success();
        }
        catch (DbException ex) when (SqlExceptionHelper.IsUniqueConstraintViolation(ex, "UQ_academic_participations_student_stage_period"))
        {
            return Result.Failure(ParticipationErrors.DuplicateParticipation(
                participation.StudentId,
                participation.SubjectStageId,
                participation.OperationalPeriodId));
        }
    }

    public async Task<int> AddBatchAsync(IEnumerable<AcademicParticipation> participations, CancellationToken cancellationToken = default)
    {
        var list = participations.ToList();
        if (list.Count == 0)
        {
            return 0;
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        var inserted = 0;
        foreach (var p in list)
        {
            var rowsAffected = await connection.ExecuteAsync(
                new CommandDefinition(
                    InsertIfNotExistsSql,
                    new
                    {
                        p.Id,
                        p.StudentId,
                        p.StudentName,
                        p.StudentNis,
                        p.SubjectStageId,
                        p.SubjectStageName,
                        p.SubjectId,
                        p.SubjectName,
                        p.OperationalPeriodId,
                        p.OperationalPeriodName,
                        p.AcademicYearId,
                        p.AcademicYearName,
                        p.StudyGroupId,
                        p.StudyGroupName,
                        Status = p.Status.ToCode(),
                        p.CreatedAt,
                        p.UpdatedAt
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            if (rowsAffected > 0)
            {
                inserted++;
            }
        }

        transaction.Commit();
        return inserted;
    }

    public async Task<Result> UpdateAsync(AcademicParticipation participation, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var rowsAffected = await connection.ExecuteAsync(
            new CommandDefinition(
                UpdateSql,
                new
                {
                    participation.Id,
                    Status = participation.Status.ToCode(),
                    participation.StudyGroupId,
                    participation.StudyGroupName,
                    participation.UpdatedAt
                },
                cancellationToken: cancellationToken));

        return rowsAffected > 0
            ? Result.Success()
            : Result.Failure(ParticipationErrors.ParticipationNotFound(participation.Id));
    }

    private static AcademicParticipation MapToDomain(ParticipationRow row)
    {
        var statusResult = ParticipationStatusExtensions.ParseParticipationStatus(row.status);
        var status = statusResult.IsSuccess ? statusResult.Value : ParticipationStatus.Active;

        return AcademicParticipation.Rehydrate(
            row.id,
            row.student_id,
            row.student_name,
            row.student_nis,
            row.subject_stage_id,
            row.subject_stage_name,
            row.subject_id,
            row.subject_name,
            row.operational_period_id,
            row.operational_period_name,
            row.academic_year_id,
            row.academic_year_name,
            row.study_group_id,
            row.study_group_name,
            status,
            row.created_at,
            row.updated_at);
    }

    private sealed class ParticipationRow
    {
        public Guid id { get; set; }
        public Guid student_id { get; set; }
        public string student_name { get; set; } = string.Empty;
        public string student_nis { get; set; } = string.Empty;
        public Guid subject_stage_id { get; set; }
        public string subject_stage_name { get; set; } = string.Empty;
        public Guid subject_id { get; set; }
        public string subject_name { get; set; } = string.Empty;
        public Guid operational_period_id { get; set; }
        public string operational_period_name { get; set; } = string.Empty;
        public Guid academic_year_id { get; set; }
        public string academic_year_name { get; set; } = string.Empty;
        public Guid? study_group_id { get; set; }
        public string? study_group_name { get; set; }
        public string status { get; set; } = string.Empty;
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}
