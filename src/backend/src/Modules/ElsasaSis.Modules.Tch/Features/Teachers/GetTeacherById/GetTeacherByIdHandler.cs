using Dapper;
using ElsasaSis.Modules.Tch.Domain;
using ElsasaSis.Modules.Tch.Features.Assignments;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Tch.Features.Teachers.GetTeacherById;

public sealed class GetTeacherByIdHandler(ISqlConnectionFactory connectionFactory)
{
    private const string SelectTeacherSql = """
        SELECT
            id,
            full_name,
            nickname,
            nip,
            nik,
            date_of_birth,
            gender,
            phone,
            email,
            address,
            status,
            created_at,
            updated_at
        FROM tch.teachers
        WHERE id = @Id;
        """;

    private const string SelectCapabilitiesSql = """
        SELECT id, teacher_id, subject_id, subject_name, created_at, updated_at
        FROM tch.teaching_capabilities
        WHERE teacher_id = @TeacherId
        ORDER BY subject_name ASC;
        """;

    private const string SelectActiveAssignmentsSql = """
        SELECT
            id,
            teacher_id,
            teacher_name,
            academic_year_id,
            academic_year_name,
            operational_period_id,
            operational_period_name,
            subject_id,
            subject_name,
            study_group_id,
            study_group_name,
            status,
            created_at,
            updated_at
        FROM tch.teacher_assignments
        WHERE teacher_id = @TeacherId
          AND status = 'ASSIGNED'
        ORDER BY academic_year_name DESC, operational_period_name ASC, study_group_name ASC, subject_name ASC;
        """;

    public async Task<Result<TeacherDetailDto>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<TeacherRow>(
            new CommandDefinition(SelectTeacherSql, new { Id = id }, cancellationToken: cancellationToken));

        if (row is null)
        {
            return Result<TeacherDetailDto>.Failure(TchErrors.TeacherNotFound(id));
        }

        var capabilityRows = await connection.QueryAsync<CapabilityRow>(
            new CommandDefinition(SelectCapabilitiesSql, new { TeacherId = id }, cancellationToken: cancellationToken));

        var assignmentRows = await connection.QueryAsync<AssignmentRow>(
            new CommandDefinition(SelectActiveAssignmentsSql, new { TeacherId = id }, cancellationToken: cancellationToken));

        var capabilities = capabilityRows
            .Select(c => new TeachingCapabilityDto(
                c.id,
                c.teacher_id,
                c.subject_id,
                c.subject_name,
                DateTime.SpecifyKind(c.created_at, DateTimeKind.Utc),
                DateTime.SpecifyKind(c.updated_at, DateTimeKind.Utc)))
            .ToArray();

        var assignments = assignmentRows
            .Select(a => new TeacherAssignmentDto(
                a.id,
                a.teacher_id,
                a.teacher_name,
                a.academic_year_id,
                a.academic_year_name,
                a.operational_period_id,
                a.operational_period_name,
                a.subject_id,
                a.subject_name,
                a.study_group_id,
                a.study_group_name,
                a.status,
                DateTime.SpecifyKind(a.created_at, DateTimeKind.Utc),
                DateTime.SpecifyKind(a.updated_at, DateTimeKind.Utc)))
            .ToArray();

        return Result<TeacherDetailDto>.Success(new TeacherDetailDto(
            row.id,
            row.full_name,
            row.nickname,
            row.nip,
            row.nik,
            DateOnly.FromDateTime(row.date_of_birth),
            row.gender,
            row.phone,
            row.email,
            row.address,
            row.status,
            assignments.Length,
            DateTime.SpecifyKind(row.created_at, DateTimeKind.Utc),
            DateTime.SpecifyKind(row.updated_at, DateTimeKind.Utc),
            capabilities,
            assignments));
    }

    private sealed class TeacherRow
    {
        public Guid id { get; init; }
        public string full_name { get; init; } = string.Empty;
        public string? nickname { get; init; }
        public string? nip { get; init; }
        public string? nik { get; init; }
        public DateTime date_of_birth { get; init; }
        public string gender { get; init; } = string.Empty;
        public string? phone { get; init; }
        public string? email { get; init; }
        public string? address { get; init; }
        public string status { get; init; } = string.Empty;
        public DateTime created_at { get; init; }
        public DateTime updated_at { get; init; }
    }

    private sealed class CapabilityRow
    {
        public Guid id { get; init; }
        public Guid teacher_id { get; init; }
        public Guid subject_id { get; init; }
        public string subject_name { get; init; } = string.Empty;
        public DateTime created_at { get; init; }
        public DateTime updated_at { get; init; }
    }

    private sealed class AssignmentRow
    {
        public Guid id { get; init; }
        public Guid teacher_id { get; init; }
        public string teacher_name { get; init; } = string.Empty;
        public Guid academic_year_id { get; init; }
        public string academic_year_name { get; init; } = string.Empty;
        public Guid operational_period_id { get; init; }
        public string operational_period_name { get; init; } = string.Empty;
        public Guid subject_id { get; init; }
        public string subject_name { get; init; } = string.Empty;
        public Guid study_group_id { get; init; }
        public string study_group_name { get; init; } = string.Empty;
        public string status { get; init; } = string.Empty;
        public DateTime created_at { get; init; }
        public DateTime updated_at { get; init; }
    }
}
