using System.Data.Common;
using Dapper;
using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.Modules.Acd.Domain.Model;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Acd.Infrastructure.Persistence;

public sealed class SubjectStageRepository(ISqlConnectionFactory connectionFactory) : ISubjectStageRepository
{
    private const string SelectStageByIdSql = """
        SELECT id, subject_id, subject_name, name, sequence_order, description, created_at, updated_at
        FROM acd.subject_stages
        WHERE id = @Id;
        """;

    private const string SelectUnitsByStageIdSql = """
        SELECT id, subject_stage_id, name, sequence_order, unit_type, description, created_at, updated_at
        FROM acd.learning_units
        WHERE subject_stage_id = @StageId
        ORDER BY sequence_order;
        """;

    private const string SelectGradesByStageIdSql = """
        SELECT id, subject_stage_id, school_level, grade, created_at, updated_at
        FROM acd.subject_stage_grades
        WHERE subject_stage_id = @StageId
        ORDER BY school_level, grade;
        """;

    private const string ExistsByNameSql = """
        SELECT CASE WHEN EXISTS (
            SELECT 1 FROM acd.subject_stages
            WHERE subject_id = @SubjectId
              AND LOWER(LTRIM(RTRIM(name))) = LOWER(LTRIM(RTRIM(@Name)))
              AND (@ExcludeId IS NULL OR id <> @ExcludeId)
        ) THEN 1 ELSE 0 END;
        """;

    private const string ExistsBySequenceOrderSql = """
        SELECT CASE WHEN EXISTS (
            SELECT 1 FROM acd.subject_stages
            WHERE subject_id = @SubjectId
              AND sequence_order = @SequenceOrder
              AND (@ExcludeId IS NULL OR id <> @ExcludeId)
        ) THEN 1 ELSE 0 END;
        """;

    private const string InsertStageSql = """
        INSERT INTO acd.subject_stages (id, subject_id, subject_name, name, sequence_order, description, created_at, updated_at)
        VALUES (@Id, @SubjectId, @SubjectName, @Name, @SequenceOrder, @Description, @CreatedAt, @UpdatedAt);
        """;

    private const string UpdateStageSql = """
        UPDATE acd.subject_stages
        SET name = @Name,
            sequence_order = @SequenceOrder,
            description = @Description,
            updated_at = @UpdatedAt
        WHERE id = @Id;
        """;

    private const string InsertUnitSql = """
        INSERT INTO acd.learning_units (id, subject_stage_id, name, sequence_order, unit_type, description, created_at, updated_at)
        VALUES (@Id, @SubjectStageId, @Name, @SequenceOrder, @UnitType, @Description, @CreatedAt, @UpdatedAt);
        """;

    private const string UpdateUnitSql = """
        UPDATE acd.learning_units
        SET name = @Name,
            sequence_order = @SequenceOrder,
            unit_type = @UnitType,
            description = @Description,
            updated_at = @UpdatedAt
        WHERE id = @Id;
        """;

    private const string DeleteUnitsByIdsSql = """
        DELETE FROM acd.learning_units
        WHERE id IN @Ids;
        """;

    private const string InsertGradeSql = """
        INSERT INTO acd.subject_stage_grades (id, subject_stage_id, school_level, grade, created_at, updated_at)
        VALUES (@Id, @SubjectStageId, @SchoolLevel, @Grade, @CreatedAt, @UpdatedAt);
        """;

    private const string UpdateGradeSql = """
        UPDATE acd.subject_stage_grades
        SET school_level = @SchoolLevel,
            grade = @Grade,
            updated_at = @UpdatedAt
        WHERE id = @Id;
        """;

    private const string DeleteGradesByIdsSql = """
        DELETE FROM acd.subject_stage_grades
        WHERE id IN @Ids;
        """;

    public async Task<SubjectStage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var stageRow = await connection.QuerySingleOrDefaultAsync<SubjectStageRow>(
            new CommandDefinition(SelectStageByIdSql, new { Id = id }, cancellationToken: cancellationToken));

        if (stageRow is null)
        {
            return null;
        }

        var unitRows = await connection.QueryAsync<LearningUnitRow>(
            new CommandDefinition(SelectUnitsByStageIdSql, new { StageId = id }, cancellationToken: cancellationToken));

        var gradeRows = await connection.QueryAsync<SubjectStageGradeRow>(
            new CommandDefinition(SelectGradesByStageIdSql, new { StageId = id }, cancellationToken: cancellationToken));

        var units = unitRows.Select(u => LearningUnit.Rehydrate(
            u.id,
            u.subject_stage_id,
            u.name,
            u.sequence_order,
            u.unit_type,
            u.description,
            DateTime.SpecifyKind(u.created_at, DateTimeKind.Utc),
            DateTime.SpecifyKind(u.updated_at, DateTimeKind.Utc)));

        var grades = gradeRows.Select(g => SubjectStageGrade.Rehydrate(
            g.id,
            g.subject_stage_id,
            g.school_level,
            g.grade,
            DateTime.SpecifyKind(g.created_at, DateTimeKind.Utc),
            DateTime.SpecifyKind(g.updated_at, DateTimeKind.Utc)));

        return SubjectStage.Rehydrate(
            stageRow.id,
            stageRow.subject_id,
            stageRow.subject_name,
            stageRow.name,
            stageRow.sequence_order,
            stageRow.description,
            DateTime.SpecifyKind(stageRow.created_at, DateTimeKind.Utc),
            DateTime.SpecifyKind(stageRow.updated_at, DateTimeKind.Utc),
            units,
            grades);
    }

    public async Task<bool> ExistsByNameAsync(
        Guid subjectId,
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                ExistsByNameSql,
                new { SubjectId = subjectId, Name = name, ExcludeId = excludeId },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> ExistsBySequenceOrderAsync(
        Guid subjectId,
        int sequenceOrder,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                ExistsBySequenceOrderSql,
                new { SubjectId = subjectId, SequenceOrder = sequenceOrder, ExcludeId = excludeId },
                cancellationToken: cancellationToken));
    }

    public async Task<Result> AddAsync(SubjectStage stage, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();

            await connection.ExecuteAsync(new CommandDefinition(
                InsertStageSql,
                new
                {
                    Id = stage.Id,
                    SubjectId = stage.SubjectId,
                    SubjectName = stage.SubjectName,
                    Name = stage.Name,
                    SequenceOrder = stage.SequenceOrder,
                    Description = stage.Description,
                    CreatedAt = stage.CreatedAt,
                    UpdatedAt = stage.UpdatedAt
                },
                transaction,
                cancellationToken: cancellationToken));

            foreach (var unit in stage.LearningUnits)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    InsertUnitSql,
                    new
                    {
                        Id = unit.Id,
                        SubjectStageId = unit.SubjectStageId,
                        Name = unit.Name,
                        SequenceOrder = unit.SequenceOrder,
                        UnitType = unit.UnitType,
                        Description = unit.Description,
                        CreatedAt = unit.CreatedAt,
                        UpdatedAt = unit.UpdatedAt
                    },
                    transaction,
                    cancellationToken: cancellationToken));
            }

            foreach (var grade in stage.GradeMappings)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    InsertGradeSql,
                    new
                    {
                        Id = grade.Id,
                        SubjectStageId = grade.SubjectStageId,
                        SchoolLevel = grade.SchoolLevel,
                        Grade = grade.Grade,
                        CreatedAt = grade.CreatedAt,
                        UpdatedAt = grade.UpdatedAt
                    },
                    transaction,
                    cancellationToken: cancellationToken));
            }

            transaction.Commit();
            return Result.Success();
        }
        catch (DbException ex) when (SqlExceptionHelper.IsUniqueConstraintViolation(ex, "UQ_subject_stages_subject_name"))
        {
            return Result.Failure(LearningStructureErrors.DuplicateStageName(stage.Name));
        }
        catch (DbException ex) when (SqlExceptionHelper.IsUniqueConstraintViolation(ex, "UQ_subject_stages_subject_seq"))
        {
            return Result.Failure(LearningStructureErrors.DuplicateStageSequence(stage.SequenceOrder));
        }
        catch (DbException ex) when (SqlExceptionHelper.IsUniqueConstraintViolation(ex, "UQ_learning_units_stage_name"))
        {
            return Result.Failure(LearningStructureErrors.DuplicateUnitName(string.Empty));
        }
        catch (DbException ex) when (SqlExceptionHelper.IsUniqueConstraintViolation(ex, "UQ_learning_units_stage_seq"))
        {
            return Result.Failure(LearningStructureErrors.DuplicateUnitSequence(0));
        }
        catch (DbException ex) when (SqlExceptionHelper.IsUniqueConstraintViolation(ex, "UQ_subject_stage_grades_stage_level_grade"))
        {
            return Result.Failure(LearningStructureErrors.DuplicateGradeMapping(string.Empty, string.Empty));
        }
    }

    public async Task<Result> UpdateAsync(SubjectStage stage, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();

            var stageRowsAffected = await connection.ExecuteAsync(new CommandDefinition(
                UpdateStageSql,
                new
                {
                    Id = stage.Id,
                    Name = stage.Name,
                    SequenceOrder = stage.SequenceOrder,
                    Description = stage.Description,
                    UpdatedAt = stage.UpdatedAt
                },
                transaction,
                cancellationToken: cancellationToken));

            if (stageRowsAffected == 0)
            {
                return Result.Failure(LearningStructureErrors.SubjectStageNotFound(stage.Id));
            }

            // Sync Learning Units
            var existingUnitIds = (await connection.QueryAsync<Guid>(
                new CommandDefinition(
                    "SELECT id FROM acd.learning_units WHERE subject_stage_id = @StageId;",
                    new { StageId = stage.Id },
                    transaction,
                    cancellationToken: cancellationToken))).ToHashSet();

            var currentUnitIds = stage.LearningUnits.Select(u => u.Id).ToHashSet();
            var unitsToDelete = existingUnitIds.Except(currentUnitIds).ToList();

            if (unitsToDelete.Count > 0)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    DeleteUnitsByIdsSql,
                    new { Ids = unitsToDelete },
                    transaction,
                    cancellationToken: cancellationToken));
            }

            foreach (var unit in stage.LearningUnits)
            {
                if (existingUnitIds.Contains(unit.Id))
                {
                    await connection.ExecuteAsync(new CommandDefinition(
                        UpdateUnitSql,
                        new
                        {
                            Id = unit.Id,
                            Name = unit.Name,
                            SequenceOrder = unit.SequenceOrder,
                            UnitType = unit.UnitType,
                            Description = unit.Description,
                            UpdatedAt = unit.UpdatedAt
                        },
                        transaction,
                        cancellationToken: cancellationToken));
                }
                else
                {
                    await connection.ExecuteAsync(new CommandDefinition(
                        InsertUnitSql,
                        new
                        {
                            Id = unit.Id,
                            SubjectStageId = unit.SubjectStageId,
                            Name = unit.Name,
                            SequenceOrder = unit.SequenceOrder,
                            UnitType = unit.UnitType,
                            Description = unit.Description,
                            CreatedAt = unit.CreatedAt,
                            UpdatedAt = unit.UpdatedAt
                        },
                        transaction,
                        cancellationToken: cancellationToken));
                }
            }

            // Sync Grade Mappings
            var existingGradeIds = (await connection.QueryAsync<Guid>(
                new CommandDefinition(
                    "SELECT id FROM acd.subject_stage_grades WHERE subject_stage_id = @StageId;",
                    new { StageId = stage.Id },
                    transaction,
                    cancellationToken: cancellationToken))).ToHashSet();

            var currentGradeIds = stage.GradeMappings.Select(g => g.Id).ToHashSet();
            var gradesToDelete = existingGradeIds.Except(currentGradeIds).ToList();

            if (gradesToDelete.Count > 0)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    DeleteGradesByIdsSql,
                    new { Ids = gradesToDelete },
                    transaction,
                    cancellationToken: cancellationToken));
            }

            foreach (var grade in stage.GradeMappings)
            {
                if (existingGradeIds.Contains(grade.Id))
                {
                    await connection.ExecuteAsync(new CommandDefinition(
                        UpdateGradeSql,
                        new
                        {
                            Id = grade.Id,
                            SchoolLevel = grade.SchoolLevel,
                            Grade = grade.Grade,
                            UpdatedAt = grade.UpdatedAt
                        },
                        transaction,
                        cancellationToken: cancellationToken));
                }
                else
                {
                    await connection.ExecuteAsync(new CommandDefinition(
                        InsertGradeSql,
                        new
                        {
                            Id = grade.Id,
                            SubjectStageId = grade.SubjectStageId,
                            SchoolLevel = grade.SchoolLevel,
                            Grade = grade.Grade,
                            CreatedAt = grade.CreatedAt,
                            UpdatedAt = grade.UpdatedAt
                        },
                        transaction,
                        cancellationToken: cancellationToken));
                }
            }

            transaction.Commit();
            return Result.Success();
        }
        catch (DbException ex) when (SqlExceptionHelper.IsUniqueConstraintViolation(ex, "UQ_subject_stages_subject_name"))
        {
            return Result.Failure(LearningStructureErrors.DuplicateStageName(stage.Name));
        }
        catch (DbException ex) when (SqlExceptionHelper.IsUniqueConstraintViolation(ex, "UQ_subject_stages_subject_seq"))
        {
            return Result.Failure(LearningStructureErrors.DuplicateStageSequence(stage.SequenceOrder));
        }
        catch (DbException ex) when (SqlExceptionHelper.IsUniqueConstraintViolation(ex, "UQ_learning_units_stage_name"))
        {
            return Result.Failure(LearningStructureErrors.DuplicateUnitName(string.Empty));
        }
        catch (DbException ex) when (SqlExceptionHelper.IsUniqueConstraintViolation(ex, "UQ_learning_units_stage_seq"))
        {
            return Result.Failure(LearningStructureErrors.DuplicateUnitSequence(0));
        }
        catch (DbException ex) when (SqlExceptionHelper.IsUniqueConstraintViolation(ex, "UQ_subject_stage_grades_stage_level_grade"))
        {
            return Result.Failure(LearningStructureErrors.DuplicateGradeMapping(string.Empty, string.Empty));
        }
    }

    private sealed record SubjectStageRow(
        Guid id,
        Guid subject_id,
        string subject_name,
        string name,
        int sequence_order,
        string? description,
        DateTime created_at,
        DateTime updated_at);

    private sealed record LearningUnitRow(
        Guid id,
        Guid subject_stage_id,
        string name,
        int sequence_order,
        string unit_type,
        string? description,
        DateTime created_at,
        DateTime updated_at);

    private sealed record SubjectStageGradeRow(
        Guid id,
        Guid subject_stage_id,
        string school_level,
        string grade,
        DateTime created_at,
        DateTime updated_at);
}
