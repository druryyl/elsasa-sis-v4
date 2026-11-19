using Dapper;
using ElsasaSis.Contracts.Operational;
using ElsasaSis.Modules.Tch.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Tch.Features.Assignments.GetTeacherWorkload;

public sealed class GetTeacherWorkloadHandler(
    ISqlConnectionFactory connectionFactory,
    IOperationalContract operationalContract)
{
    private const string SelectTeachersSql = """
        SELECT id, full_name, nip, status
        FROM tch.teachers
        WHERE status = 'ACTIVE'
           OR (
               @OperationalPeriodId IS NOT NULL
               AND EXISTS (
                   SELECT 1
                   FROM tch.teacher_assignments ta
                   WHERE ta.teacher_id = tch.teachers.id
                     AND ta.operational_period_id = @OperationalPeriodId
                     AND ta.status = 'ASSIGNED'
               )
           )
        ORDER BY full_name ASC;
        """;

    private const string SelectAssignmentsSql = """
        SELECT
            id,
            teacher_id,
            subject_id,
            subject_name,
            study_group_id,
            study_group_name
        FROM tch.teacher_assignments
        WHERE operational_period_id = @OperationalPeriodId
          AND status = 'ASSIGNED'
        ORDER BY study_group_name ASC, subject_name ASC;
        """;

    public async Task<Result<TeacherWorkloadResponse>> HandleAsync(
        GetTeacherWorkloadQuery query,
        CancellationToken cancellationToken = default)
    {
        Guid? operationalPeriodId = null;
        string? operationalPeriodName = null;
        Guid? academicYearId = null;
        string? academicYearName = null;

        if (query.OperationalPeriodId.HasValue)
        {
            var period = await operationalContract.GetOperationalPeriodByIdAsync(
                query.OperationalPeriodId.Value,
                cancellationToken);

            if (period is null)
            {
                return Result<TeacherWorkloadResponse>.Failure(
                    TchErrors.OperationalPeriodNotFound(query.OperationalPeriodId.Value));
            }

            operationalPeriodId = period.Id;
            operationalPeriodName = period.Name;
            academicYearId = period.AcademicYearId;
            academicYearName = period.AcademicYearName;
        }
        else
        {
            var activePeriod = await operationalContract.GetActiveOperationalPeriodAsync(cancellationToken);
            if (activePeriod is not null)
            {
                operationalPeriodId = activePeriod.Id;
                operationalPeriodName = activePeriod.Name;
                academicYearId = activePeriod.AcademicYearId;

                var academicYear = await operationalContract.GetAcademicYearByIdAsync(
                    activePeriod.AcademicYearId,
                    cancellationToken);

                academicYearName = academicYear?.Name;
            }
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var teacherRows = (await connection.QueryAsync<TeacherSummaryRow>(
            new CommandDefinition(
                SelectTeachersSql,
                new { OperationalPeriodId = operationalPeriodId },
                cancellationToken: cancellationToken))).ToArray();

        var assignmentRows = operationalPeriodId.HasValue
            ? (await connection.QueryAsync<AssignmentSummaryRow>(
                new CommandDefinition(
                    SelectAssignmentsSql,
                    new { OperationalPeriodId = operationalPeriodId.Value },
                    cancellationToken: cancellationToken))).ToArray()
            : [];

        var assignmentsByTeacher = assignmentRows
            .GroupBy(a => a.teacher_id)
            .ToDictionary(g => g.Key, g => g.ToArray());

        var teacherItems = teacherRows
            .Select(teacher =>
            {
                var teacherAssignments = assignmentsByTeacher.TryGetValue(teacher.id, out var list)
                    ? list
                    : [];

                var studyGroups = teacherAssignments
                    .GroupBy(a => (a.study_group_id, a.study_group_name))
                    .Select(g => new WorkloadStudyGroupDto(
                        g.Key.study_group_id,
                        g.Key.study_group_name,
                        g.Count()))
                    .OrderBy(g => g.StudyGroupName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var subjects = teacherAssignments
                    .GroupBy(a => (a.subject_id, a.subject_name))
                    .Select(g => new WorkloadSubjectDto(
                        g.Key.subject_id,
                        g.Key.subject_name,
                        g.Count()))
                    .OrderBy(s => s.SubjectName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var items = teacherAssignments
                    .Select(a => new WorkloadAssignmentItemDto(
                        a.id,
                        a.subject_id,
                        a.subject_name,
                        a.study_group_id,
                        a.study_group_name))
                    .ToArray();

                return new TeacherWorkloadItemDto(
                    teacher.id,
                    teacher.full_name,
                    teacher.nip,
                    teacher.status,
                    teacherAssignments.Length,
                    studyGroups.Length,
                    subjects.Length,
                    studyGroups,
                    subjects,
                    items);
            })
            .OrderByDescending(t => t.TotalAssignments)
            .ThenBy(t => t.FullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var activeTeachersCount = teacherItems.Count(t =>
            string.Equals(t.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase));

        var assignedTeachersCount = teacherItems.Count(t => t.TotalAssignments > 0);
        var unassignedTeachersCount = teacherItems.Count(t =>
            string.Equals(t.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) && t.TotalAssignments == 0);

        var distinctStudyGroupsCount = assignmentRows.Select(a => a.study_group_id).Distinct().Count();
        var distinctSubjectsCount = assignmentRows.Select(a => a.subject_id).Distinct().Count();

        return Result<TeacherWorkloadResponse>.Success(new TeacherWorkloadResponse(
            operationalPeriodId,
            operationalPeriodName,
            academicYearId,
            academicYearName,
            teacherItems.Length,
            activeTeachersCount,
            assignedTeachersCount,
            unassignedTeachersCount,
            assignmentRows.Length,
            distinctStudyGroupsCount,
            distinctSubjectsCount,
            teacherItems));
    }

    private sealed class TeacherSummaryRow
    {
        public Guid id { get; init; }
        public string full_name { get; init; } = string.Empty;
        public string? nip { get; init; }
        public string status { get; init; } = string.Empty;
    }

    private sealed class AssignmentSummaryRow
    {
        public Guid id { get; init; }
        public Guid teacher_id { get; init; }
        public Guid subject_id { get; init; }
        public string subject_name { get; init; } = string.Empty;
        public Guid study_group_id { get; init; }
        public string study_group_name { get; init; } = string.Empty;
    }
}
