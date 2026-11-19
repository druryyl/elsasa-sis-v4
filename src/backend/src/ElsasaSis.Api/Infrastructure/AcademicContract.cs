using Dapper;
using ElsasaSis.Contracts.Academic;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Api.Infrastructure;

public sealed class AcademicContract(ISqlConnectionFactory connectionFactory) : IAcademicContract
{
    private const string GetAllSubjectsSql = """
        SELECT id, name, code
        FROM acd.subjects
        ORDER BY name;
        """;

    private const string GetSubjectByIdSql = """
        SELECT id, name, code
        FROM acd.subjects
        WHERE id = @Id;
        """;

    public async Task<IReadOnlyList<SubjectDto>> GetAllSubjectsAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<SubjectRow>(
            new CommandDefinition(GetAllSubjectsSql, cancellationToken: cancellationToken));

        return rows
            .Select(r => new SubjectDto(r.id, r.name, r.code))
            .ToList();
    }

    public async Task<SubjectDto?> GetSubjectByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<SubjectRow>(
            new CommandDefinition(GetSubjectByIdSql, new { Id = id }, cancellationToken: cancellationToken));

        return row is null
            ? null
            : new SubjectDto(row.id, row.name, row.code);
    }

    private sealed record SubjectRow(
        Guid id,
        string name,
        string? code);
}
