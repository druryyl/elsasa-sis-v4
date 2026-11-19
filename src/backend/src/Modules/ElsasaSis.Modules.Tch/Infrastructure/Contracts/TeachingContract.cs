using Dapper;
using ElsasaSis.Contracts.Teaching;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Tch.Infrastructure.Contracts;

public sealed class TeachingContract(ISqlConnectionFactory connectionFactory) : ITeachingContract
{
    private const string TeacherByIdSql = """
        SELECT id, full_name, status
        FROM tch.teachers
        WHERE id = @Id;
        """;

    public async Task<TeacherSummaryDto?> GetTeacherByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<TeacherSummaryRow>(
            new CommandDefinition(TeacherByIdSql, new { Id = id }, cancellationToken: cancellationToken));

        return row is null
            ? null
            : new TeacherSummaryDto(row.id, row.full_name, row.status);
    }

    private sealed record TeacherSummaryRow(
        Guid id,
        string full_name,
        string status);
}
