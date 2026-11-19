using System.Globalization;
using Dapper;
using ElsasaSis.Modules.Std.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Std.Features.Admissions.GetNextNis;

public sealed class GetNextNisHandler(ISqlConnectionFactory connectionFactory)
{
    private const string CandidateExistsSql = """
        SELECT CASE WHEN EXISTS (
            SELECT 1
            FROM std.candidates
            WHERE id = @Id
        ) THEN 1 ELSE 0 END;
        """;

    private const string NextSequentialNisSql = """
        SELECT COALESCE(MAX(CAST(RIGHT(nis, 4) AS INT)), 0) + 1
        FROM std.students
        WHERE LEN(nis) = 8
          AND nis LIKE @YearPrefix + '[0-9][0-9][0-9][0-9]';
        """;

    public async Task<Result<NextNisResponse>> HandleAsync(
        Guid id,
        DateOnly? enrollmentDate = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var exists = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(CandidateExistsSql, new { Id = id }, cancellationToken: cancellationToken));

        if (!exists)
        {
            return Result<NextNisResponse>.Failure(StdErrors.CandidateNotFound(id));
        }

        var year = enrollmentDate?.Year ?? DateOnly.FromDateTime(DateTime.UtcNow).Year;
        var yearPrefix = year.ToString("0000", CultureInfo.InvariantCulture);

        var nextSequence = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(NextSequentialNisSql, new { YearPrefix = yearPrefix }, cancellationToken: cancellationToken));

        var suggestedNis = $"{yearPrefix}{nextSequence.ToString("D4", CultureInfo.InvariantCulture)}";
        return Result<NextNisResponse>.Success(new NextNisResponse(suggestedNis));
    }
}
