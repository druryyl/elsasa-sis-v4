using Dapper;
using ElsasaSis.Modules.Std.Domain;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Std.Infrastructure.Persistence;

public sealed class CandidateRepository(ISqlConnectionFactory connectionFactory) : ICandidateRepository
{
    private const string SelectCandidateSql = """
        SELECT id, admission_year_id, admission_year_name, full_name, nickname,
               date_of_birth, gender, phone, email, address,
               target_school_level, target_grade, admission_status,
               registered_at, decided_at, decision_notes, enrolled_student_id,
               created_at, updated_at
        FROM std.candidates
        WHERE id = @Id;
        """;

    private const string SelectCandidateGuardiansSql = """
        SELECT id, candidate_id, full_name, relationship_type, phone, email, address, created_at, updated_at
        FROM std.candidate_guardians
        WHERE candidate_id = @CandidateId
        ORDER BY relationship_type ASC, full_name ASC, id ASC;
        """;

    private const string InsertCandidateSql = """
        INSERT INTO std.candidates (
            id, admission_year_id, admission_year_name, full_name, nickname,
            date_of_birth, gender, phone, email, address,
            target_school_level, target_grade, admission_status,
            registered_at, decided_at, decision_notes, enrolled_student_id,
            created_at, updated_at
        )
        VALUES (
            @Id, @AdmissionYearId, @AdmissionYearName, @FullName, @Nickname,
            @DateOfBirth, @Gender, @Phone, @Email, @Address,
            @TargetSchoolLevel, @TargetGrade, @AdmissionStatus,
            @RegisteredAt, @DecidedAt, @DecisionNotes, @EnrolledStudentId,
            @CreatedAt, @UpdatedAt
        );
        """;

    private const string InsertCandidateGuardianSql = """
        INSERT INTO std.candidate_guardians (
            id, candidate_id, full_name, relationship_type, phone, email, address, created_at, updated_at
        )
        VALUES (
            @Id, @CandidateId, @FullName, @RelationshipType, @Phone, @Email, @Address, @CreatedAt, @UpdatedAt
        );
        """;

    private const string UpdateCandidateSql = """
        UPDATE std.candidates
        SET admission_year_id = @AdmissionYearId,
            admission_year_name = @AdmissionYearName,
            full_name = @FullName,
            nickname = @Nickname,
            date_of_birth = @DateOfBirth,
            gender = @Gender,
            phone = @Phone,
            email = @Email,
            address = @Address,
            target_school_level = @TargetSchoolLevel,
            target_grade = @TargetGrade,
            admission_status = @AdmissionStatus,
            decided_at = @DecidedAt,
            decision_notes = @DecisionNotes,
            enrolled_student_id = @EnrolledStudentId,
            updated_at = @UpdatedAt
        WHERE id = @Id;
        """;

    public async Task<Candidate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<CandidateRow>(
            new CommandDefinition(SelectCandidateSql, new { Id = id }, cancellationToken: cancellationToken));

        if (row is null)
        {
            return null;
        }

        var guardianRows = await connection.QueryAsync<CandidateGuardianRow>(
            new CommandDefinition(SelectCandidateGuardiansSql, new { CandidateId = id }, cancellationToken: cancellationToken));

        var guardians = guardianRows.Select(g => CandidateGuardian.Rehydrate(
            g.id,
            g.candidate_id,
            g.full_name,
            Enum.Parse<GuardianRelationshipType>(g.relationship_type, ignoreCase: true),
            g.phone,
            g.email,
            g.address,
            SqlPersistenceHelper.SpecifyUtc(g.created_at),
            SqlPersistenceHelper.SpecifyUtc(g.updated_at)));

        return Candidate.Rehydrate(
            row.id,
            row.admission_year_id,
            row.admission_year_name,
            row.full_name,
            row.nickname,
            DateOnly.FromDateTime(row.date_of_birth),
            Enum.Parse<Gender>(row.gender, ignoreCase: true),
            row.phone,
            row.email,
            row.address,
            Enum.Parse<SchoolLevel>(row.target_school_level, ignoreCase: true),
            row.target_grade,
            Enum.Parse<AdmissionStatus>(row.admission_status, ignoreCase: true),
            SqlPersistenceHelper.SpecifyUtc(row.registered_at),
            SqlPersistenceHelper.SpecifyNullableUtc(row.decided_at),
            row.decision_notes,
            row.enrolled_student_id,
            SqlPersistenceHelper.SpecifyUtc(row.created_at),
            SqlPersistenceHelper.SpecifyUtc(row.updated_at),
            guardians);
    }

    public Task AddAsync(Candidate candidate, CancellationToken cancellationToken = default) =>
        SqlPersistenceHelper.ExecuteInTransactionAsync(connectionFactory, async (connection, transaction, ct) =>
        {
            await connection.ExecuteAsync(new CommandDefinition(
                InsertCandidateSql,
                ToCandidateParameters(candidate),
                transaction,
                cancellationToken: ct));

            foreach (var guardian in candidate.Guardians)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    InsertCandidateGuardianSql,
                    ToGuardianParameters(guardian),
                    transaction,
                    cancellationToken: ct));
            }
        }, cancellationToken);

    public Task UpdateAsync(Candidate candidate, CancellationToken cancellationToken = default) =>
        SqlPersistenceHelper.ExecuteInTransactionAsync(connectionFactory, async (connection, transaction, ct) =>
        {
            await connection.ExecuteAsync(new CommandDefinition(
                UpdateCandidateSql,
                ToCandidateParameters(candidate),
                transaction,
                cancellationToken: ct));
        }, cancellationToken);

    private static object ToCandidateParameters(Candidate candidate) => new
    {
        candidate.Id,
        candidate.AdmissionYearId,
        candidate.AdmissionYearName,
        candidate.FullName,
        candidate.Nickname,
        DateOfBirth = candidate.DateOfBirth.ToDateTime(TimeOnly.MinValue),
        Gender = candidate.Gender.ToCode(),
        candidate.Phone,
        candidate.Email,
        candidate.Address,
        TargetSchoolLevel = candidate.TargetSchoolLevel.ToCode(),
        candidate.TargetGrade,
        AdmissionStatus = candidate.AdmissionStatus.ToCode(),
        candidate.RegisteredAt,
        candidate.DecidedAt,
        candidate.DecisionNotes,
        candidate.EnrolledStudentId,
        candidate.CreatedAt,
        candidate.UpdatedAt
    };

    private static object ToGuardianParameters(CandidateGuardian guardian) => new
    {
        guardian.Id,
        guardian.CandidateId,
        guardian.FullName,
        RelationshipType = guardian.RelationshipType.ToCode(),
        guardian.Phone,
        guardian.Email,
        guardian.Address,
        guardian.CreatedAt,
        guardian.UpdatedAt
    };

    private sealed class CandidateRow
    {
        public Guid id { get; set; }
        public Guid admission_year_id { get; set; }
        public string admission_year_name { get; set; } = string.Empty;
        public string full_name { get; set; } = string.Empty;
        public string? nickname { get; set; }
        public DateTime date_of_birth { get; set; }
        public string gender { get; set; } = string.Empty;
        public string? phone { get; set; }
        public string? email { get; set; }
        public string? address { get; set; }
        public string target_school_level { get; set; } = string.Empty;
        public string target_grade { get; set; } = string.Empty;
        public string admission_status { get; set; } = string.Empty;
        public DateTime registered_at { get; set; }
        public DateTime? decided_at { get; set; }
        public string? decision_notes { get; set; }
        public Guid? enrolled_student_id { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    private sealed class CandidateGuardianRow
    {
        public Guid id { get; set; }
        public Guid candidate_id { get; set; }
        public string full_name { get; set; } = string.Empty;
        public string relationship_type { get; set; } = string.Empty;
        public string? phone { get; set; }
        public string? email { get; set; }
        public string? address { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}
