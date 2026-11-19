using Dapper;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Api.Features.Academic.GetSubjects;

public sealed class GetSubjectsHandler(ISqlConnectionFactory connectionFactory)
{
    private const string Sql = """
        SELECT id, name, code
        FROM acd.subjects
        ORDER BY name;
        """;

    public async Task<IReadOnlyList<SubjectResponse>> HandleAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<SubjectRow>(
            new CommandDefinition(Sql, cancellationToken: cancellationToken));

        return rows
            .Select(r => new SubjectResponse(r.id, r.name, r.code))
            .ToList();
    }

    private sealed record SubjectRow(
        Guid id,
        string name,
        string? code);
}
