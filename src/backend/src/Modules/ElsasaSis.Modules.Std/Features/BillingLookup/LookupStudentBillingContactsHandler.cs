using Dapper;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Std.Features.BillingLookup;

public sealed class GetStudentBillingContactsHandler(ISqlConnectionFactory connectionFactory)
{
    private const string SelectBillingContactsSql = """
        WITH RankedPlacements AS (
            SELECT sp.student_id,
                   sp.school_level,
                   sp.grade,
                   sp.study_group_id,
                   sp.study_group_name,
                   ROW_NUMBER() OVER (
                       PARTITION BY sp.student_id
                       ORDER BY sp.created_at DESC
                   ) AS rn
            FROM std.student_placements sp
        ),
        RankedGuardians AS (
            SELECT sg.student_id,
                   sg.full_name AS guardian_name,
                   sg.relationship_type,
                   sg.phone,
                   ROW_NUMBER() OVER (
                       PARTITION BY sg.student_id
                       ORDER BY CASE LOWER(sg.relationship_type)
                                    WHEN 'father' THEN 1
                                    WHEN 'mother' THEN 2
                                    ELSE 3
                                END,
                                sg.created_at ASC
                   ) AS rn
            FROM std.student_guardians sg
        )
        SELECT s.id AS StudentId,
               s.nis AS StudentNis,
               s.full_name AS StudentName,
               rp.school_level AS SchoolLevel,
               rp.grade AS Grade,
               rp.study_group_id AS StudyGroupId,
               rp.study_group_name AS StudyGroupName,
               rg.guardian_name AS PrimaryGuardianName,
               rg.phone AS PhoneNumber,
               rg.relationship_type AS Relationship
        FROM std.students s
        LEFT JOIN RankedPlacements rp ON rp.student_id = s.id AND rp.rn = 1
        LEFT JOIN RankedGuardians rg ON rg.student_id = s.id AND rg.rn = 1
        WHERE s.status = 'ACTIVE'
          AND (
              @SearchPattern IS NULL
              OR s.full_name LIKE @SearchPattern
              OR s.nis LIKE @SearchPattern
              OR rg.guardian_name LIKE @SearchPattern
              OR rg.phone LIKE @SearchPattern
              OR rp.study_group_name LIKE @SearchPattern
          )
        ORDER BY s.full_name ASC;
        """;

    public async Task<Result<IReadOnlyList<StudentBillingContactDto>>> HandleAsync(
        LookupStudentBillingContactsQuery query,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var searchPattern = string.IsNullOrWhiteSpace(query.Search)
            ? null
            : $"%{query.Search.Trim()}%";

        var rows = await connection.QueryAsync<BillingContactRow>(
            new CommandDefinition(
                SelectBillingContactsSql,
                new { SearchPattern = searchPattern },
                cancellationToken: cancellationToken));

        var dtos = rows.Select(r => new StudentBillingContactDto(
            r.StudentId,
            r.StudentNis,
            r.StudentName,
            r.SchoolLevel,
            r.Grade,
            r.StudyGroupId,
            r.StudyGroupName,
            r.PrimaryGuardianName,
            r.PhoneNumber,
            r.Relationship,
            FormatWhatsAppUrl(r.PhoneNumber))).ToList();

        return Result<IReadOnlyList<StudentBillingContactDto>>.Success(dtos);
    }

    public static string? FormatWhatsAppUrl(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(digitsOnly))
        {
            return null;
        }

        if (digitsOnly.StartsWith("0", StringComparison.Ordinal))
        {
            digitsOnly = "62" + digitsOnly[1..];
        }
        else if (!digitsOnly.StartsWith("62", StringComparison.Ordinal))
        {
            digitsOnly = "62" + digitsOnly;
        }

        return $"https://wa.me/{digitsOnly}";
    }

    private sealed class BillingContactRow
    {
        public Guid StudentId { get; set; }
        public string StudentNis { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string? SchoolLevel { get; set; }
        public string? Grade { get; set; }
        public Guid? StudyGroupId { get; set; }
        public string? StudyGroupName { get; set; }
        public string? PrimaryGuardianName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Relationship { get; set; }
    }
}
