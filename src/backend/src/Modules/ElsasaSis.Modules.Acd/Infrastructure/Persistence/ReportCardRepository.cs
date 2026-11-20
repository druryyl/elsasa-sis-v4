using Dapper;
using ElsasaSis.Modules.Acd.Domain.ReportCards;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Acd.Infrastructure.Persistence;

public sealed class ReportCardRepository(ISqlConnectionFactory connectionFactory) : IReportCardRepository
{
    private const string SelectByIdSql = """
        SELECT 
            id,
            student_id,
            student_name,
            student_nis,
            student_nisn,
            academic_year_id,
            academic_year_name,
            operational_period_id,
            operational_period_name,
            study_group_id,
            study_group_name,
            homeroom_teacher_id,
            homeroom_teacher_name,
            status,
            override_reason,
            general_remarks,
            finalized_at,
            created_at,
            updated_at
        FROM acd.report_cards
        WHERE id = @Id;
        """;

    private const string SelectByStudentAndPeriodSql = """
        SELECT 
            id,
            student_id,
            student_name,
            student_nis,
            student_nisn,
            academic_year_id,
            academic_year_name,
            operational_period_id,
            operational_period_name,
            study_group_id,
            study_group_name,
            homeroom_teacher_id,
            homeroom_teacher_name,
            status,
            override_reason,
            general_remarks,
            finalized_at,
            created_at,
            updated_at
        FROM acd.report_cards
        WHERE student_id = @StudentId AND operational_period_id = @PeriodId;
        """;

    private const string SelectEntriesByCardIdSql = """
        SELECT 
            id,
            report_card_id,
            subject_id,
            subject_name,
            subject_code,
            subject_stage_id,
            subject_stage_name,
            teacher_id,
            teacher_name,
            numeric_score,
            letter_grade,
            teacher_notes,
            created_at,
            updated_at
        FROM acd.report_card_entries
        WHERE report_card_id = @ReportCardId
        ORDER BY subject_name;
        """;

    private const string InsertReportCardSql = """
        INSERT INTO acd.report_cards (
            id,
            student_id,
            student_name,
            student_nis,
            student_nisn,
            academic_year_id,
            academic_year_name,
            operational_period_id,
            operational_period_name,
            study_group_id,
            study_group_name,
            homeroom_teacher_id,
            homeroom_teacher_name,
            status,
            override_reason,
            general_remarks,
            finalized_at,
            created_at,
            updated_at
        )
        VALUES (
            @Id,
            @StudentId,
            @StudentName,
            @StudentNis,
            @StudentNisn,
            @AcademicYearId,
            @AcademicYearName,
            @OperationalPeriodId,
            @OperationalPeriodName,
            @StudyGroupId,
            @StudyGroupName,
            @HomeroomTeacherId,
            @HomeroomTeacherName,
            @Status,
            @OverrideReason,
            @GeneralRemarks,
            @FinalizedAt,
            @CreatedAt,
            @UpdatedAt
        );
        """;

    private const string InsertEntrySql = """
        INSERT INTO acd.report_card_entries (
            id,
            report_card_id,
            subject_id,
            subject_name,
            subject_code,
            subject_stage_id,
            subject_stage_name,
            teacher_id,
            teacher_name,
            numeric_score,
            letter_grade,
            teacher_notes,
            created_at,
            updated_at
        )
        VALUES (
            @Id,
            @ReportCardId,
            @SubjectId,
            @SubjectName,
            @SubjectCode,
            @SubjectStageId,
            @SubjectStageName,
            @TeacherId,
            @TeacherName,
            @NumericScore,
            @LetterGrade,
            @TeacherNotes,
            @CreatedAt,
            @UpdatedAt
        );
        """;

    private const string UpdateReportCardSql = """
        UPDATE acd.report_cards
        SET status = @Status,
            override_reason = @OverrideReason,
            general_remarks = @GeneralRemarks,
            finalized_at = @FinalizedAt,
            updated_at = @UpdatedAt
        WHERE id = @Id;
        """;

    private const string UpdateEntryNotesSql = """
        UPDATE acd.report_card_entries
        SET teacher_id = @TeacherId,
            teacher_name = @TeacherName,
            teacher_notes = @TeacherNotes,
            updated_at = @UpdatedAt
        WHERE id = @Id;
        """;

    public async Task<ReportCard?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);

        var row = await connection.QuerySingleOrDefaultAsync<ReportCardRow>(
            new CommandDefinition(SelectByIdSql, new { Id = id }, cancellationToken: ct));

        return row is null ? null : MapToReportCard(row);
    }

    public async Task<ReportCard?> GetByStudentAndPeriodAsync(Guid studentId, Guid periodId, CancellationToken ct = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);

        var row = await connection.QuerySingleOrDefaultAsync<ReportCardRow>(
            new CommandDefinition(SelectByStudentAndPeriodSql, new { StudentId = studentId, PeriodId = periodId }, cancellationToken: ct));

        return row is null ? null : MapToReportCard(row);
    }

    public async Task<IReadOnlyList<ReportCardEntry>> GetEntriesByReportCardIdAsync(Guid reportCardId, CancellationToken ct = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);

        var rows = await connection.QueryAsync<ReportCardEntryRow>(
            new CommandDefinition(SelectEntriesByCardIdSql, new { ReportCardId = reportCardId }, cancellationToken: ct));

        return rows.Select(MapToReportCardEntry).ToList();
    }

    public async Task InsertAsync(ReportCard reportCard, IEnumerable<ReportCardEntry> entries, CancellationToken ct = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        using var transaction = connection.BeginTransaction();

        await connection.ExecuteAsync(
            new CommandDefinition(
                InsertReportCardSql,
                new
                {
                    reportCard.Id,
                    reportCard.StudentId,
                    reportCard.StudentName,
                    reportCard.StudentNis,
                    reportCard.StudentNisn,
                    reportCard.AcademicYearId,
                    reportCard.AcademicYearName,
                    reportCard.OperationalPeriodId,
                    reportCard.OperationalPeriodName,
                    reportCard.StudyGroupId,
                    reportCard.StudyGroupName,
                    reportCard.HomeroomTeacherId,
                    reportCard.HomeroomTeacherName,
                    reportCard.Status,
                    reportCard.OverrideReason,
                    reportCard.GeneralRemarks,
                    reportCard.FinalizedAt,
                    reportCard.CreatedAt,
                    reportCard.UpdatedAt
                },
                transaction: transaction,
                cancellationToken: ct));

        foreach (var entry in entries)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    InsertEntrySql,
                    new
                    {
                        entry.Id,
                        entry.ReportCardId,
                        entry.SubjectId,
                        entry.SubjectName,
                        entry.SubjectCode,
                        entry.SubjectStageId,
                        entry.SubjectStageName,
                        entry.TeacherId,
                        entry.TeacherName,
                        entry.NumericScore,
                        entry.LetterGrade,
                        entry.TeacherNotes,
                        entry.CreatedAt,
                        entry.UpdatedAt
                    },
                    transaction: transaction,
                    cancellationToken: ct));
        }

        transaction.Commit();
    }

    public async Task UpdateAsync(ReportCard reportCard, CancellationToken ct = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);

        await connection.ExecuteAsync(
            new CommandDefinition(
                UpdateReportCardSql,
                new
                {
                    reportCard.Id,
                    reportCard.Status,
                    reportCard.OverrideReason,
                    reportCard.GeneralRemarks,
                    reportCard.FinalizedAt,
                    reportCard.UpdatedAt
                },
                cancellationToken: ct));
    }

    public async Task UpdateEntryNotesAsync(ReportCardEntry entry, CancellationToken ct = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);

        await connection.ExecuteAsync(
            new CommandDefinition(
                UpdateEntryNotesSql,
                new
                {
                    entry.Id,
                    entry.TeacherId,
                    entry.TeacherName,
                    entry.TeacherNotes,
                    entry.UpdatedAt
                },
                cancellationToken: ct));
    }

    public async Task InsertBatchAsync(
        IEnumerable<(ReportCard ReportCard, IEnumerable<ReportCardEntry> Entries)> items,
        CancellationToken ct = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        using var transaction = connection.BeginTransaction();

        foreach (var (reportCard, entries) in items)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    InsertReportCardSql,
                    new
                    {
                        reportCard.Id,
                        reportCard.StudentId,
                        reportCard.StudentName,
                        reportCard.StudentNis,
                        reportCard.StudentNisn,
                        reportCard.AcademicYearId,
                        reportCard.AcademicYearName,
                        reportCard.OperationalPeriodId,
                        reportCard.OperationalPeriodName,
                        reportCard.StudyGroupId,
                        reportCard.StudyGroupName,
                        reportCard.HomeroomTeacherId,
                        reportCard.HomeroomTeacherName,
                        reportCard.Status,
                        reportCard.OverrideReason,
                        reportCard.GeneralRemarks,
                        reportCard.FinalizedAt,
                        reportCard.CreatedAt,
                        reportCard.UpdatedAt
                    },
                    transaction: transaction,
                    cancellationToken: ct));

            foreach (var entry in entries)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        InsertEntrySql,
                        new
                        {
                            entry.Id,
                            entry.ReportCardId,
                            entry.SubjectId,
                            entry.SubjectName,
                            entry.SubjectCode,
                            entry.SubjectStageId,
                            entry.SubjectStageName,
                            entry.TeacherId,
                            entry.TeacherName,
                            entry.NumericScore,
                            entry.LetterGrade,
                            entry.TeacherNotes,
                            entry.CreatedAt,
                            entry.UpdatedAt
                        },
                        transaction: transaction,
                        cancellationToken: ct));
            }
        }

        transaction.Commit();
    }

    public async Task<IReadOnlySet<Guid>> GetExistingCardStudentIdsAsync(
        Guid operationalPeriodId,
        IEnumerable<Guid> studentIds,
        CancellationToken ct = default)
    {
        var idList = studentIds.ToList();
        if (idList.Count == 0) return new HashSet<Guid>();

        using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = """
            SELECT student_id
            FROM acd.report_cards
            WHERE operational_period_id = @OperationalPeriodId
              AND student_id IN @StudentIds;
            """;

        var existing = await connection.QueryAsync<Guid>(
            new CommandDefinition(sql, new { OperationalPeriodId = operationalPeriodId, StudentIds = idList }, cancellationToken: ct));

        return existing.ToHashSet();
    }

    public async Task<IReadOnlyDictionary<Guid, string?>> GetStudentNisnsAsync(
        IEnumerable<Guid> studentIds,
        CancellationToken ct = default)
    {
        var idList = studentIds.ToList();
        if (idList.Count == 0) return new Dictionary<Guid, string?>();

        using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = """
            SELECT id, nisn
            FROM std.students
            WHERE id IN @StudentIds;
            """;

        var rows = await connection.QueryAsync<(Guid Id, string? Nisn)>(
            new CommandDefinition(sql, new { StudentIds = idList }, cancellationToken: ct));

        return rows.ToDictionary(r => r.Id, r => r.Nisn);
    }

    public async Task<IReadOnlyList<StudentDraftEntryData>> ComputeDraftCardEntriesAsync(
        Guid studyGroupId,
        Guid operationalPeriodId,
        string schoolLevel,
        string grade,
        IEnumerable<Guid> studentIds,
        CancellationToken ct = default)
    {
        var idList = studentIds.ToList();
        if (idList.Count == 0) return Array.Empty<StudentDraftEntryData>();

        using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);

        const string participationSql = """
            SELECT 
                ap.student_id AS StudentId,
                ap.subject_id AS SubjectId,
                ap.subject_name AS SubjectName,
                s.code AS SubjectCode,
                ap.subject_stage_id AS SubjectStageId,
                ap.subject_stage_name AS SubjectStageName,
                ta.teacher_id AS TeacherId,
                ta.teacher_name AS TeacherName,
                ISNULL(ROUND(AVG(sc.score), 2), 0.00) AS NumericScore
            FROM acd.academic_participations ap
            LEFT JOIN acd.subjects s ON ap.subject_id = s.id
            LEFT JOIN tch.teacher_assignments ta 
                ON ta.study_group_id = ap.study_group_id 
               AND ta.subject_id = ap.subject_id 
               AND ta.operational_period_id = ap.operational_period_id 
               AND ta.status = 'ACTIVE'
            LEFT JOIN acd.assignments a 
                ON a.subject_stage_id = ap.subject_stage_id 
               AND a.operational_period_id = ap.operational_period_id
            LEFT JOIN acd.assignment_scores sc 
                ON sc.assignment_id = a.id 
               AND sc.student_id = ap.student_id
            WHERE ap.operational_period_id = @OperationalPeriodId
              AND ap.student_id IN @StudentIds
              AND ap.status = 'ACTIVE'
            GROUP BY 
                ap.student_id,
                ap.subject_id,
                ap.subject_name,
                s.code,
                ap.subject_stage_id,
                ap.subject_stage_name,
                ta.teacher_id,
                ta.teacher_name
            ORDER BY ap.subject_name;
            """;

        var entries = (await connection.QueryAsync<StudentDraftEntryData>(
            new CommandDefinition(participationSql, new { OperationalPeriodId = operationalPeriodId, StudentIds = idList }, cancellationToken: ct))).ToList();

        var studentsWithEntries = entries.Select(e => e.StudentId).ToHashSet();
        var missingStudentIds = idList.Where(id => !studentsWithEntries.Contains(id)).ToList();

        if (missingStudentIds.Count > 0)
        {
            const string fallbackGradeSql = """
                SELECT 
                    st.id AS StudentId,
                    ss.subject_id AS SubjectId,
                    ss.subject_name AS SubjectName,
                    s.code AS SubjectCode,
                    ss.id AS SubjectStageId,
                    ss.name AS SubjectStageName,
                    ta.teacher_id AS TeacherId,
                    ta.teacher_name AS TeacherName,
                    ISNULL(ROUND(AVG(sc.score), 2), 0.00) AS NumericScore
                FROM acd.subject_stages ss
                INNER JOIN acd.subject_stage_grades ssg ON ss.id = ssg.subject_stage_id
                CROSS JOIN (SELECT id FROM std.students WHERE id IN @MissingStudentIds) st
                LEFT JOIN acd.subjects s ON ss.subject_id = s.id
                LEFT JOIN tch.teacher_assignments ta 
                    ON ta.study_group_id = @StudyGroupId 
                   AND ta.subject_id = ss.subject_id 
                   AND ta.operational_period_id = @OperationalPeriodId 
                   AND ta.status = 'ACTIVE'
                LEFT JOIN acd.assignments a 
                    ON a.subject_stage_id = ss.id 
                   AND a.operational_period_id = @OperationalPeriodId
                LEFT JOIN acd.assignment_scores sc 
                    ON sc.assignment_id = a.id 
                   AND sc.student_id = st.id
                WHERE ssg.school_level = @SchoolLevel AND ssg.grade = @Grade
                GROUP BY 
                    st.id,
                    ss.subject_id,
                    ss.subject_name,
                    s.code,
                    ss.id,
                    ss.name,
                    ta.teacher_id,
                    ta.teacher_name
                ORDER BY ss.subject_name;
                """;

            var fallbackEntries = await connection.QueryAsync<StudentDraftEntryData>(
                new CommandDefinition(fallbackGradeSql, new
                {
                    MissingStudentIds = missingStudentIds,
                    StudyGroupId = studyGroupId,
                    OperationalPeriodId = operationalPeriodId,
                    SchoolLevel = schoolLevel,
                    Grade = grade
                }, cancellationToken: ct));

            entries.AddRange(fallbackEntries);
        }

        return entries;
    }

    public async Task<bool> HasTeacherAssignmentAsync(
        Guid teacherId,
        Guid studyGroupId,
        Guid subjectId,
        Guid operationalPeriodId,
        CancellationToken ct = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = """
            SELECT CASE WHEN EXISTS (
                SELECT 1 FROM tch.teacher_assignments
                WHERE teacher_id = @TeacherId
                  AND study_group_id = @StudyGroupId
                  AND subject_id = @SubjectId
                  AND operational_period_id = @OperationalPeriodId
                  AND status = 'ACTIVE'
            ) THEN 1 ELSE 0 END;
            """;

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new
            {
                TeacherId = teacherId,
                StudyGroupId = studyGroupId,
                SubjectId = subjectId,
                OperationalPeriodId = operationalPeriodId
            }, cancellationToken: ct));
    }

    private static ReportCard MapToReportCard(ReportCardRow row) =>
        ReportCard.Rehydrate(
            row.id,
            row.student_id,
            row.student_name,
            row.student_nis,
            row.student_nisn,
            row.academic_year_id,
            row.academic_year_name,
            row.operational_period_id,
            row.operational_period_name,
            row.study_group_id,
            row.study_group_name,
            row.homeroom_teacher_id,
            row.homeroom_teacher_name,
            row.status,
            row.override_reason,
            row.general_remarks,
            row.finalized_at,
            row.created_at,
            row.updated_at);

    private static ReportCardEntry MapToReportCardEntry(ReportCardEntryRow row) =>
        ReportCardEntry.Rehydrate(
            row.id,
            row.report_card_id,
            row.subject_id,
            row.subject_name,
            row.subject_code,
            row.subject_stage_id,
            row.subject_stage_name,
            row.teacher_id,
            row.teacher_name,
            row.numeric_score,
            row.letter_grade,
            row.teacher_notes,
            row.created_at,
            row.updated_at);

    private sealed record ReportCardRow(
        Guid id,
        Guid student_id,
        string student_name,
        string student_nis,
        string? student_nisn,
        Guid academic_year_id,
        string academic_year_name,
        Guid operational_period_id,
        string operational_period_name,
        Guid? study_group_id,
        string? study_group_name,
        Guid? homeroom_teacher_id,
        string? homeroom_teacher_name,
        string status,
        string? override_reason,
        string? general_remarks,
        DateTime? finalized_at,
        DateTime created_at,
        DateTime updated_at);

    private sealed record ReportCardEntryRow(
        Guid id,
        Guid report_card_id,
        Guid subject_id,
        string subject_name,
        string? subject_code,
        Guid subject_stage_id,
        string subject_stage_name,
        Guid? teacher_id,
        string? teacher_name,
        decimal numeric_score,
        string? letter_grade,
        string? teacher_notes,
        DateTime created_at,
        DateTime updated_at);
}
