using Dapper;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Acd.Features.LearningStructure.GetSubjects;

public sealed class GetSubjectsHandler(ISqlConnectionFactory connectionFactory)
{
    private const string HierarchySql = """
        SELECT id, name, code
        FROM acd.subjects
        ORDER BY name;

        SELECT id, subject_id, subject_name, name, sequence_order, description
        FROM acd.subject_stages
        ORDER BY subject_id, sequence_order;

        SELECT id, subject_stage_id, name, sequence_order, unit_type, description
        FROM acd.learning_units
        ORDER BY subject_stage_id, sequence_order;

        SELECT id, subject_stage_id, school_level, grade
        FROM acd.subject_stage_grades
        ORDER BY subject_stage_id, school_level, grade;
        """;

    public async Task<IReadOnlyList<SubjectResponse>> HandleAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(HierarchySql, cancellationToken: cancellationToken));

        var subjects = (await multi.ReadAsync<SubjectRow>()).ToList();
        var stages = (await multi.ReadAsync<StageRow>()).ToList();
        var units = (await multi.ReadAsync<UnitRow>()).ToList();
        var grades = (await multi.ReadAsync<GradeRow>()).ToList();

        var unitsLookup = units
            .GroupBy(u => u.subject_stage_id)
            .ToDictionary(
                g => g.Key,
                g => g.Select(u => new LearningUnitResponse(
                    u.id,
                    u.subject_stage_id,
                    u.name,
                    u.sequence_order,
                    u.unit_type,
                    u.description)).ToList());

        var gradesLookup = grades
            .GroupBy(g => g.subject_stage_id)
            .ToDictionary(
                g => g.Key,
                g => g.Select(gr => new SubjectStageGradeResponse(
                    gr.id,
                    gr.subject_stage_id,
                    gr.school_level,
                    gr.grade)).ToList());

        var stagesLookup = stages
            .GroupBy(s => s.subject_id)
            .ToDictionary(
                g => g.Key,
                g => g.Select(s => new SubjectStageResponse(
                    s.id,
                    s.subject_id,
                    s.subject_name,
                    s.name,
                    s.sequence_order,
                    s.description,
                    unitsLookup.GetValueOrDefault(s.id) ?? [],
                    gradesLookup.GetValueOrDefault(s.id) ?? [])).ToList());

        return subjects
            .Select(sub => new SubjectResponse(
                sub.id,
                sub.name,
                sub.code,
                stagesLookup.GetValueOrDefault(sub.id) ?? []))
            .ToList();
    }

    private sealed record SubjectRow(
        Guid id,
        string name,
        string? code);

    private sealed record StageRow(
        Guid id,
        Guid subject_id,
        string subject_name,
        string name,
        int sequence_order,
        string? description);

    private sealed record UnitRow(
        Guid id,
        Guid subject_stage_id,
        string name,
        int sequence_order,
        string unit_type,
        string? description);

    private sealed record GradeRow(
        Guid id,
        Guid subject_stage_id,
        string school_level,
        string grade);
}
