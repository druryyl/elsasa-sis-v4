using System.Data.Common;
using Dapper;
using ElsasaSis.Modules.Tch.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Tch.Infrastructure.Persistence;

public sealed class TeacherAssignmentRepository(ISqlConnectionFactory connectionFactory) : ITeacherAssignmentRepository
{
    private const string SelectColumns = """
        id, teacher_id, teacher_name, academic_year_id, academic_year_name,
        operational_period_id, operational_period_name, subject_id, subject_name,
        study_group_id, study_group_name, status, created_at, updated_at
        """;

    private const string SelectByIdSql = $"""
        SELECT {SelectColumns}
        FROM tch.teacher_assignments
        WHERE id = @Id;
        """;

    private const string SelectByTupleSql = $"""
        SELECT {SelectColumns}
        FROM tch.teacher_assignments
        WHERE teacher_id = @TeacherId
          AND subject_id = @SubjectId
          AND study_group_id = @StudyGroupId
          AND operational_period_id = @OperationalPeriodId;
        """;

    private const string InsertAssignmentSql = """
        INSERT INTO tch.teacher_assignments (
            id, teacher_id, teacher_name, academic_year_id, academic_year_name,
            operational_period_id, operational_period_name, subject_id, subject_name,
            study_group_id, study_group_name, status, created_at, updated_at
        )
        VALUES (
            @Id, @TeacherId, @TeacherName, @AcademicYearId, @AcademicYearName,
            @OperationalPeriodId, @OperationalPeriodName, @SubjectId, @SubjectName,
            @StudyGroupId, @StudyGroupName, @Status, @CreatedAt, @UpdatedAt
        );
        """;

    private const string UpdateAssignmentSql = """
        UPDATE tch.teacher_assignments
        SET teacher_name = @TeacherName,
            academic_year_id = @AcademicYearId,
            academic_year_name = @AcademicYearName,
            operational_period_id = @OperationalPeriodId,
            operational_period_name = @OperationalPeriodName,
            subject_id = @SubjectId,
            subject_name = @SubjectName,
            study_group_id = @StudyGroupId,
            study_group_name = @StudyGroupName,
            status = @Status,
            updated_at = @UpdatedAt
        WHERE id = @Id;
        """;

    public async Task<TeacherAssignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<TeacherAssignmentRow>(
            new CommandDefinition(SelectByIdSql, new { Id = id }, cancellationToken: cancellationToken));

        return row is null ? null : MapRow(row);
    }

    public async Task<TeacherAssignment?> GetByTupleAsync(
        Guid teacherId,
        Guid subjectId,
        Guid studyGroupId,
        Guid operationalPeriodId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<TeacherAssignmentRow>(
            new CommandDefinition(
                SelectByTupleSql,
                new
                {
                    TeacherId = teacherId,
                    SubjectId = subjectId,
                    StudyGroupId = studyGroupId,
                    OperationalPeriodId = operationalPeriodId
                },
                cancellationToken: cancellationToken));

        return row is null ? null : MapRow(row);
    }

    public async Task<Result> AddAsync(TeacherAssignment assignment, CancellationToken cancellationToken = default)
    {
        try
        {
            await SqlPersistenceHelper.ExecuteInTransactionAsync(connectionFactory, async (connection, transaction, ct) =>
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    InsertAssignmentSql,
                    ToParameters(assignment),
                    transaction,
                    cancellationToken: ct));
            }, cancellationToken);

            return Result.Success();
        }
        catch (DbException ex) when (TryMapUniqueViolation(ex, assignment, out var error))
        {
            return Result.Failure(error);
        }
    }

    public async Task<Result> UpdateAsync(TeacherAssignment assignment, CancellationToken cancellationToken = default)
    {
        try
        {
            await SqlPersistenceHelper.ExecuteInTransactionAsync(connectionFactory, async (connection, transaction, ct) =>
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    UpdateAssignmentSql,
                    ToParameters(assignment),
                    transaction,
                    cancellationToken: ct));
            }, cancellationToken);

            return Result.Success();
        }
        catch (DbException ex) when (TryMapUniqueViolation(ex, assignment, out var error))
        {
            return Result.Failure(error);
        }
    }

    private static bool TryMapUniqueViolation(DbException exception, TeacherAssignment assignment, out DomainError error)
    {
        if (SqlPersistenceHelper.IsUniqueConstraintViolation(exception) &&
            SqlPersistenceHelper.MentionsConstraint(exception, "UQ_teacher_assignments_teacher_subject_group_period"))
        {
            error = TchErrors.DuplicateAssignment(
                assignment.TeacherId,
                assignment.SubjectId,
                assignment.StudyGroupId,
                assignment.OperationalPeriodId);
            return true;
        }

        error = default!;
        return false;
    }

    private static TeacherAssignment MapRow(TeacherAssignmentRow row) =>
        TeacherAssignment.Rehydrate(
            row.id,
            row.teacher_id,
            row.teacher_name,
            row.academic_year_id,
            row.academic_year_name,
            row.operational_period_id,
            row.operational_period_name,
            row.subject_id,
            row.subject_name,
            row.study_group_id,
            row.study_group_name,
            Enum.Parse<AssignmentStatus>(row.status, ignoreCase: true),
            row.created_at,
            row.updated_at);

    private static object ToParameters(TeacherAssignment assignment) => new
    {
        assignment.Id,
        assignment.TeacherId,
        assignment.TeacherName,
        assignment.AcademicYearId,
        assignment.AcademicYearName,
        assignment.OperationalPeriodId,
        assignment.OperationalPeriodName,
        assignment.SubjectId,
        assignment.SubjectName,
        assignment.StudyGroupId,
        assignment.StudyGroupName,
        Status = assignment.Status.ToString().ToUpperInvariant(),
        assignment.CreatedAt,
        assignment.UpdatedAt
    };

    private sealed record TeacherAssignmentRow(
        Guid id,
        Guid teacher_id,
        string teacher_name,
        Guid academic_year_id,
        string academic_year_name,
        Guid operational_period_id,
        string operational_period_name,
        Guid subject_id,
        string subject_name,
        Guid study_group_id,
        string study_group_name,
        string status,
        DateTime created_at,
        DateTime updated_at);
}
