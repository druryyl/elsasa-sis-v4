using ElsasaSis.Modules.Dev.Features.Achievements;
using ElsasaSis.Modules.Dev.Features.Behaviour;
using ElsasaSis.Modules.Dev.Features.Discipline;
using ElsasaSis.Modules.Dev.Features.Dossier;
using ElsasaSis.Modules.Dev.Features.Overview;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Dev.Features;

public sealed class DevelopmentEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dev")
            .WithTags("Student Development");

        // UC-TCH-06: Record Behaviour Observation Note
        group.MapPost("/behaviour", async (
                RecordBehaviourNoteRequest request,
                RecordBehaviourNoteHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(request, ct);
                return result.IsSuccess
                    ? Results.Created($"/api/dev/behaviour/{result.Value!.Id}", result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Development.Behaviour.Record)
            .AddEndpointFilter<ValidationFilter<RecordBehaviourNoteRequest>>()
            .Produces<BehaviourNoteDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // UC-TCH-07, UC-ADM-22: Record Classroom Disciplinary Incident
        group.MapPost("/discipline", async (
                RecordDisciplineIncidentRequest request,
                RecordDisciplineIncidentHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(request, ct);
                return result.IsSuccess
                    ? Results.Created($"/api/dev/discipline/{result.Value!.Id}", result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Development.Discipline.Record)
            .AddEndpointFilter<ValidationFilter<RecordDisciplineIncidentRequest>>()
            .Produces<DisciplineRecordDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // UC-PRC-03: Record Escalated Sanction (Principal only)
        group.MapPost("/discipline/escalated", async (
                RecordEscalatedSanctionRequest request,
                RecordEscalatedSanctionHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(request, ct);
                return result.IsSuccess
                    ? Results.Created($"/api/dev/discipline/{result.Value!.Id}", result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Development.Discipline.Escalate)
            .AddEndpointFilter<ValidationFilter<RecordEscalatedSanctionRequest>>()
            .Produces<DisciplineRecordDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // UC-TCH-08, UC-ADM-23: Record Student Achievement Award
        group.MapPost("/achievements", async (
                RecordAchievementRequest request,
                RecordAchievementHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(request, ct);
                return result.IsSuccess
                    ? Results.Created($"/api/dev/achievements/{result.Value!.Id}", result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Development.Achievements.Record)
            .AddEndpointFilter<ValidationFilter<RecordAchievementRequest>>()
            .Produces<AchievementDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // UC-ADM-24, UC-HRM-04, UC-PRC-04: Unified 360-degree Student Development Dossier
        group.MapGet("/students/{studentId:guid}/dossier", async (
                Guid studentId,
                GetStudentDossierHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(studentId, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Development.Dossier.View)
            .Produces<StudentDossierDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        // Principal and Administrator Student Development Overview Dashboard
        group.MapGet("/overview", async (
                GetDevelopmentOverviewHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Policies.Development.Pastoral.RecordOrView)
            .Produces<DevelopmentOverviewDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
