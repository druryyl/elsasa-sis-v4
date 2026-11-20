using Dapper;
using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.Modules.Acd.Domain.Model;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Acd.Infrastructure.Persistence;

public sealed class AssignmentRepository(ISqlConnectionFactory connectionFactory) : IAssignmentRepository
{
    private const string AssignmentAndScoresSql = """
        SELECT 
            id,
            subject_stage_id,
            subject_stage_name,
            subject_id,
            subject_name,
            learning_unit_id,
            learning_unit_name,
            operational_period_id,
            operational_period_name,
            study_group_id,
            study_group_name,
            title,
            assignment_type,
            max_score,
            weight,
            assigned_date,
            due_date,
            teacher_id,
            teacher_name,
            created_at,
            updated_at
        FROM acd.assignments
        WHERE id = @Id;

        SELECT 
            id,
            assignment_id,
            student_id,
            student_name,
            student_nis,
            score,
            feedback,
            graded_at,
            created_at,
            updated_at
        FROM acd.assignment_scores
        WHERE assignment_id = @Id;
        """;

    private const string InsertAssignmentSql = """
        INSERT INTO acd.assignments (
            id,
            subject_stage_id,
            subject_stage_name,
            subject_id,
            subject_name,
            learning_unit_id,
            learning_unit_name,
            operational_period_id,
            operational_period_name,
            study_group_id,
            study_group_name,
            title,
            assignment_type,
            max_score,
            weight,
            assigned_date,
            due_date,
            teacher_id,
            teacher_name,
            created_at,
            updated_at
        )
        VALUES (
            @Id,
            @SubjectStageId,
            @SubjectStageName,
            @SubjectId,
            @SubjectName,
            @LearningUnitId,
            @LearningUnitName,
            @OperationalPeriodId,
            @OperationalPeriodName,
            @StudyGroupId,
            @StudyGroupName,
            @Title,
            @AssignmentType,
            @MaxScore,
            @Weight,
            @AssignedDate,
            @DueDate,
            @TeacherId,
            @TeacherName,
            @CreatedAt,
            @UpdatedAt
        );
        """;

    private const string UpdateAssignmentSql = """
        UPDATE acd.assignments
        SET title = @Title,
            assignment_type = @AssignmentType,
            max_score = @MaxScore,
            weight = @Weight,
            assigned_date = @AssignedDate,
            due_date = @DueDate,
            learning_unit_id = @LearningUnitId,
            learning_unit_name = @LearningUnitName,
            updated_at = @UpdatedAt
        WHERE id = @Id;
        """;

    private const string UpsertScoreSql = """
        UPDATE acd.assignment_scores WITH (UPDLOCK, HOLDLOCK)
        SET student_name = @StudentName,
            student_nis = @StudentNis,
            score = @Score,
            feedback = @Feedback,
            graded_at = @GradedAt,
            updated_at = @UpdatedAt
        WHERE assignment_id = @AssignmentId
          AND student_id = @StudentId;

        IF @@ROWCOUNT = 0
        BEGIN
            INSERT INTO acd.assignment_scores (
                id,
                assignment_id,
                student_id,
                student_name,
                student_nis,
                score,
                feedback,
                graded_at,
                created_at,
                updated_at
            )
            VALUES (
                @Id,
                @AssignmentId,
                @StudentId,
                @StudentName,
                @StudentNis,
                @Score,
                @Feedback,
                @GradedAt,
                @CreatedAt,
                @UpdatedAt
            );
        END;
        """;

    private const string HasTeacherAssignmentSql = """
        SELECT CASE WHEN EXISTS (
            SELECT 1 FROM tch.teacher_assignments
            WHERE teacher_id = @TeacherId
              AND study_group_id = @StudyGroupId
              AND subject_id = @SubjectId
              AND operational_period_id = @OperationalPeriodId
              AND status = 'ACTIVE'
        ) THEN 1 ELSE 0 END;
        """;

    private const string ActiveTeacherAssignmentSql = """
        SELECT TOP (1)
            teacher_id AS TeacherId,
            teacher_name AS TeacherName,
            subject_id AS SubjectId,
            subject_name AS SubjectName,
            study_group_id AS StudyGroupId,
            study_group_name AS StudyGroupName,
            operational_period_id AS OperationalPeriodId,
            operational_period_name AS OperationalPeriodName
        FROM tch.teacher_assignments
        WHERE study_group_id = @StudyGroupId
          AND subject_id = @SubjectId
          AND operational_period_id = @OperationalPeriodId
          AND status = 'ACTIVE';
        """;

    private const string ActiveParticipationsSql = """
        SELECT 
            student_id AS StudentId,
            student_name AS StudentName,
            student_nis AS StudentNis
        FROM acd.academic_participations
        WHERE subject_stage_id = @SubjectStageId
          AND study_group_id = @StudyGroupId
          AND operational_period_id = @OperationalPeriodId
          AND status = 'ACTIVE'
        ORDER BY student_name;
        """;

    public async Task<Assignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(AssignmentAndScoresSql, new { Id = id }, cancellationToken: cancellationToken));

        var assignmentRow = await multi.ReadSingleOrDefaultAsync<AssignmentRow>();
        if (assignmentRow is null)
        {
            return null;
        }

        var scoreRows = (await multi.ReadAsync<ScoreRow>()).ToList();

        var scores = scoreRows.Select(s => AssignmentScore.Rehydrate(
            s.id,
            s.assignment_id,
            s.student_id,
            s.student_name,
            s.student_nis,
            s.score,
            s.feedback,
            s.graded_at,
            s.created_at,
            s.updated_at)).ToList();

        var assignmentTypeResult = AssignmentTypeExtensions.ParseAssignmentType(assignmentRow.assignment_type);
        var assignmentType = assignmentTypeResult.IsSuccess ? assignmentTypeResult.Value : AssignmentType.Formative;

        return Assignment.Rehydrate(
            assignmentRow.id,
            assignmentRow.subject_stage_id,
            assignmentRow.subject_stage_name,
            assignmentRow.subject_id,
            assignmentRow.subject_name,
            assignmentRow.learning_unit_id,
            assignmentRow.learning_unit_name,
            assignmentRow.operational_period_id,
            assignmentRow.operational_period_name,
            assignmentRow.study_group_id,
            assignmentRow.study_group_name,
            assignmentRow.title,
            assignmentType,
            assignmentRow.max_score,
            assignmentRow.weight,
            DateOnly.FromDateTime(assignmentRow.assigned_date),
            assignmentRow.due_date.HasValue ? DateOnly.FromDateTime(assignmentRow.due_date.Value) : null,
            assignmentRow.teacher_id,
            assignmentRow.teacher_name,
            assignmentRow.created_at,
            assignmentRow.updated_at,
            scores);
    }

    public async Task AddAsync(Assignment assignment, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            InsertAssignmentSql,
            new
            {
                assignment.Id,
                assignment.SubjectStageId,
                assignment.SubjectStageName,
                assignment.SubjectId,
                assignment.SubjectName,
                assignment.LearningUnitId,
                assignment.LearningUnitName,
                assignment.OperationalPeriodId,
                assignment.OperationalPeriodName,
                assignment.StudyGroupId,
                assignment.StudyGroupName,
                assignment.Title,
                AssignmentType = assignment.AssignmentType.ToCode(),
                assignment.MaxScore,
                assignment.Weight,
                AssignedDate = assignment.AssignedDate.ToDateTime(TimeOnly.MinValue),
                DueDate = assignment.DueDate.HasValue ? assignment.DueDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                assignment.TeacherId,
                assignment.TeacherName,
                assignment.CreatedAt,
                assignment.UpdatedAt
            },
            cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(Assignment assignment, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            UpdateAssignmentSql,
            new
            {
                assignment.Id,
                assignment.Title,
                AssignmentType = assignment.AssignmentType.ToCode(),
                assignment.MaxScore,
                assignment.Weight,
                AssignedDate = assignment.AssignedDate.ToDateTime(TimeOnly.MinValue),
                DueDate = assignment.DueDate.HasValue ? assignment.DueDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                assignment.LearningUnitId,
                assignment.LearningUnitName,
                assignment.UpdatedAt
            },
            cancellationToken: cancellationToken));
    }

    public Task UpsertScoresAsync(
        Guid assignmentId,
        IReadOnlyList<AssignmentScore> scores,
        CancellationToken cancellationToken = default) =>
        SqlPersistenceHelper.ExecuteInTransactionAsync(
            connectionFactory,
            async (connection, transaction, ct) =>
            {
                foreach (var score in scores)
                {
                    await connection.ExecuteAsync(new CommandDefinition(
                        UpsertScoreSql,
                        new
                        {
                            score.Id,
                            AssignmentId = assignmentId,
                            score.StudentId,
                            score.StudentName,
                            score.StudentNis,
                            score.Score,
                            score.Feedback,
                            score.GradedAt,
                            score.CreatedAt,
                            score.UpdatedAt
                        },
                        transaction,
                        cancellationToken: ct));
                }
            },
            cancellationToken);

    public async Task<bool> HasTeacherAssignmentAsync(
        Guid teacherId,
        Guid studyGroupId,
        Guid subjectId,
        Guid operationalPeriodId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            HasTeacherAssignmentSql,
            new
            {
                TeacherId = teacherId,
                StudyGroupId = studyGroupId,
                SubjectId = subjectId,
                OperationalPeriodId = operationalPeriodId
            },
            cancellationToken: cancellationToken));
    }

    public async Task<TeacherAssignmentSnapshotDto?> GetActiveTeacherAssignmentAsync(
        Guid studyGroupId,
        Guid subjectId,
        Guid operationalPeriodId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<TeacherAssignmentSnapshotDto>(new CommandDefinition(
            ActiveTeacherAssignmentSql,
            new
            {
                StudyGroupId = studyGroupId,
                SubjectId = subjectId,
                OperationalPeriodId = operationalPeriodId
            },
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<ActiveParticipationSnapshotDto>> GetActiveParticipationsAsync(
        Guid subjectStageId,
        Guid studyGroupId,
        Guid operationalPeriodId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<ActiveParticipationSnapshotDto>(new CommandDefinition(
            ActiveParticipationsSql,
            new
            {
                SubjectStageId = subjectStageId,
                StudyGroupId = studyGroupId,
                OperationalPeriodId = operationalPeriodId
            },
            cancellationToken: cancellationToken));

        return rows.ToList();
    }

    private sealed record AssignmentRow(
        Guid id,
        Guid subject_stage_id,
        string subject_stage_name,
        Guid subject_id,
        string subject_name,
        Guid? learning_unit_id,
        string? learning_unit_name,
        Guid operational_period_id,
        string operational_period_name,
        Guid? study_group_id,
        string? study_group_name,
        string title,
        string assignment_type,
        decimal max_score,
        decimal? weight,
        DateTime assigned_date,
        DateTime? due_date,
        Guid teacher_id,
        string teacher_name,
        DateTime created_at,
        DateTime updated_at);

    private sealed record ScoreRow(
        Guid id,
        Guid assignment_id,
        Guid student_id,
        string student_name,
        string student_nis,
        decimal score,
        string? feedback,
        DateTime graded_at,
        DateTime created_at,
        DateTime updated_at);
}
