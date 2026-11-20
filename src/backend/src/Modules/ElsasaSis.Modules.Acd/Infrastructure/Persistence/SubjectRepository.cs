using System.Data.Common;
using Dapper;
using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.Modules.Acd.Domain.Model;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Acd.Infrastructure.Persistence;

public sealed class SubjectRepository(ISqlConnectionFactory connectionFactory) : ISubjectRepository
{
    private const string SelectByIdSql = """
        SELECT id, name, code, created_at, updated_at
        FROM acd.subjects
        WHERE id = @Id;
        """;

    private const string ExistsByNameSql = """
        SELECT CASE WHEN EXISTS (
            SELECT 1 FROM acd.subjects
            WHERE LOWER(LTRIM(RTRIM(name))) = LOWER(LTRIM(RTRIM(@Name)))
              AND (@ExcludeId IS NULL OR id <> @ExcludeId)
        ) THEN 1 ELSE 0 END;
        """;

    private const string ExistsByCodeSql = """
        SELECT CASE WHEN EXISTS (
            SELECT 1 FROM acd.subjects
            WHERE code IS NOT NULL
              AND LOWER(LTRIM(RTRIM(code))) = LOWER(LTRIM(RTRIM(@Code)))
              AND (@ExcludeId IS NULL OR id <> @ExcludeId)
        ) THEN 1 ELSE 0 END;
        """;

    private const string InsertSql = """
        INSERT INTO acd.subjects (id, name, code, created_at, updated_at)
        VALUES (@Id, @Name, @Code, @CreatedAt, @UpdatedAt);
        """;

    private const string UpdateSql = """
        UPDATE acd.subjects
        SET name = @Name,
            code = @Code,
            updated_at = @UpdatedAt
        WHERE id = @Id;
        """;

    public async Task<Subject?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<SubjectRow>(
            new CommandDefinition(SelectByIdSql, new { Id = id }, cancellationToken: cancellationToken));

        if (row is null)
        {
            return null;
        }

        return Subject.Rehydrate(
            row.id,
            row.name,
            row.code,
            isActive: true,
            DateTime.SpecifyKind(row.created_at, DateTimeKind.Utc),
            DateTime.SpecifyKind(row.updated_at, DateTimeKind.Utc));
    }

    public async Task<bool> ExistsByNameAsync(
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                ExistsByNameSql,
                new { Name = name, ExcludeId = excludeId },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> ExistsByCodeAsync(
        string code,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                ExistsByCodeSql,
                new { Code = code, ExcludeId = excludeId },
                cancellationToken: cancellationToken));
    }

    public async Task<Result> AddAsync(Subject subject, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

            await connection.ExecuteAsync(new CommandDefinition(
                InsertSql,
                new
                {
                    Id = subject.Id,
                    Name = subject.Name,
                    Code = subject.Code,
                    CreatedAt = subject.CreatedAt,
                    UpdatedAt = subject.UpdatedAt
                },
                cancellationToken: cancellationToken));

            return Result.Success();
        }
        catch (DbException ex) when (SqlExceptionHelper.IsUniqueConstraintViolation(ex, "UQ_subjects_name"))
        {
            return Result.Failure(LearningStructureErrors.DuplicateSubjectName(subject.Name));
        }
        catch (DbException ex) when (SqlExceptionHelper.IsUniqueConstraintViolation(ex, "UQ_subjects_code"))
        {
            return Result.Failure(LearningStructureErrors.DuplicateSubjectCode(subject.Code ?? string.Empty));
        }
    }

    public async Task<Result> UpdateAsync(Subject subject, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

            var rowsAffected = await connection.ExecuteAsync(new CommandDefinition(
                UpdateSql,
                new
                {
                    Id = subject.Id,
                    Name = subject.Name,
                    Code = subject.Code,
                    UpdatedAt = subject.UpdatedAt
                },
                cancellationToken: cancellationToken));

            if (rowsAffected == 0)
            {
                return Result.Failure(LearningStructureErrors.SubjectNotFound(subject.Id));
            }

            return Result.Success();
        }
        catch (DbException ex) when (SqlExceptionHelper.IsUniqueConstraintViolation(ex, "UQ_subjects_name"))
        {
            return Result.Failure(LearningStructureErrors.DuplicateSubjectName(subject.Name));
        }
        catch (DbException ex) when (SqlExceptionHelper.IsUniqueConstraintViolation(ex, "UQ_subjects_code"))
        {
            return Result.Failure(LearningStructureErrors.DuplicateSubjectCode(subject.Code ?? string.Empty));
        }
    }

    private sealed record SubjectRow(
        Guid id,
        string name,
        string? code,
        DateTime created_at,
        DateTime updated_at);
}
