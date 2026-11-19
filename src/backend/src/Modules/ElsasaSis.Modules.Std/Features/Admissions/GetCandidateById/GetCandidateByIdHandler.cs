using Dapper;
using ElsasaSis.Modules.Std.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Std.Features.Admissions.GetCandidateById;

public sealed class GetCandidateByIdHandler(ISqlConnectionFactory connectionFactory)
{
    private const string SelectCandidateSql = """
        SELECT
            id,
            admission_year_id,
            admission_year_name,
            full_name,
            nickname,
            date_of_birth,
            gender,
            phone,
            email,
            address,
            target_school_level,
            target_grade,
            admission_status,
            registered_at,
            decided_at,
            decision_notes,
            enrolled_student_id,
            created_at,
            updated_at
        FROM std.candidates
        WHERE id = @Id;
        """;

    private const string SelectGuardiansSql = """
        SELECT
            id,
            candidate_id,
            full_name,
            relationship_type,
            phone,
            email,
            address,
            created_at,
            updated_at
        FROM std.candidate_guardians
        WHERE candidate_id = @CandidateId
        ORDER BY relationship_type ASC, full_name ASC, id ASC;
        """;

    public async Task<Result<CandidateDto>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<CandidateRow>(
            new CommandDefinition(SelectCandidateSql, new { Id = id }, cancellationToken: cancellationToken));

        if (row is null)
        {
            return Result<CandidateDto>.Failure(StdErrors.CandidateNotFound(id));
        }

        var guardianRows = await connection.QueryAsync<CandidateGuardianRow>(
            new CommandDefinition(SelectGuardiansSql, new { CandidateId = id }, cancellationToken: cancellationToken));

        var guardians = guardianRows
            .Select(g => new CandidateGuardianDto(
                g.id,
                g.candidate_id,
                g.full_name,
                g.relationship_type,
                g.phone,
                g.email,
                g.address,
                AdmissionsQueryHelper.SpecifyUtc(g.created_at),
                AdmissionsQueryHelper.SpecifyUtc(g.updated_at)))
            .ToArray();

        return Result<CandidateDto>.Success(new CandidateDto(
            row.id,
            row.admission_year_id,
            row.admission_year_name,
            row.full_name,
            row.nickname,
            DateOnly.FromDateTime(row.date_of_birth),
            row.gender,
            row.phone,
            row.email,
            row.address,
            row.target_school_level,
            row.target_grade,
            row.admission_status,
            AdmissionsQueryHelper.SpecifyUtc(row.registered_at),
            AdmissionsQueryHelper.SpecifyNullableUtc(row.decided_at),
            row.decision_notes,
            row.enrolled_student_id,
            AdmissionsQueryHelper.SpecifyUtc(row.created_at),
            AdmissionsQueryHelper.SpecifyUtc(row.updated_at),
            guardians));
    }

    private sealed class CandidateRow
    {
        public Guid id { get; init; }
        public Guid admission_year_id { get; init; }
        public string admission_year_name { get; init; } = string.Empty;
        public string full_name { get; init; } = string.Empty;
        public string? nickname { get; init; }
        public DateTime date_of_birth { get; init; }
        public string gender { get; init; } = string.Empty;
        public string? phone { get; init; }
        public string? email { get; init; }
        public string? address { get; init; }
        public string target_school_level { get; init; } = string.Empty;
        public string target_grade { get; init; } = string.Empty;
        public string admission_status { get; init; } = string.Empty;
        public DateTime registered_at { get; init; }
        public DateTime? decided_at { get; init; }
        public string? decision_notes { get; init; }
        public Guid? enrolled_student_id { get; init; }
        public DateTime created_at { get; init; }
        public DateTime updated_at { get; init; }
    }

    private sealed class CandidateGuardianRow
    {
        public Guid id { get; init; }
        public Guid candidate_id { get; init; }
        public string full_name { get; init; } = string.Empty;
        public string relationship_type { get; init; } = string.Empty;
        public string? phone { get; init; }
        public string? email { get; init; }
        public string? address { get; init; }
        public DateTime created_at { get; init; }
        public DateTime updated_at { get; init; }
    }
}
