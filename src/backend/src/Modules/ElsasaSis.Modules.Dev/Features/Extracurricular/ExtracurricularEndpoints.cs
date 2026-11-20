using ElsasaSis.Modules.Dev.Features.Extracurricular.CompleteExtracurricularParticipant;
using ElsasaSis.Modules.Dev.Features.Extracurricular.CreateExtracurricularActivity;
using ElsasaSis.Modules.Dev.Features.Extracurricular.EnrollExtracurricularParticipant;
using ElsasaSis.Modules.Dev.Features.Extracurricular.GetExtracurricularActivities;
using ElsasaSis.Modules.Dev.Features.Extracurricular.GetExtracurricularParticipants;
using ElsasaSis.Modules.Dev.Features.Extracurricular.RecordExtracurricularSessionLog;
using ElsasaSis.Modules.Dev.Features.Extracurricular.UpdateExtracurricularActivity;
using ElsasaSis.Modules.Dev.Features.Extracurricular.WithdrawExtracurricularParticipant;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Dev.Features.Extracurricular;

public sealed class ExtracurricularEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dev/extracurriculars")
            .WithTags("Development - Extracurricular Activities");

        group.MapPost("/", async (
                CreateExtracurricularActivityRequest request,
                CreateExtracurricularActivityHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(request, ct);
                return result.IsSuccess
                    ? Results.Created($"/api/dev/extracurriculars/{result.Value!.Id}", result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Development.Extracurricular.Manage)
            .AddEndpointFilter<ValidationFilter<CreateExtracurricularActivityRequest>>()
            .Produces<ExtracurricularActivityDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/", async (
                [AsParameters] GetExtracurricularActivitiesQuery query,
                GetExtracurricularActivitiesHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(query, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(policy => policy.RequireAssertion(ctx =>
                ctx.User.Claims.Any(c => c.Type == "permissions" &&
                    (string.Equals(c.Value, Permissions.Development.Extracurricular.Manage, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(c.Value, Permissions.Development.Extracurricular.Log, StringComparison.OrdinalIgnoreCase)))))
            .Produces<IReadOnlyList<ExtracurricularActivityDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateExtracurricularActivityRequest request,
                UpdateExtracurricularActivityHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, request, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Development.Extracurricular.Manage)
            .AddEndpointFilter<ValidationFilter<UpdateExtracurricularActivityRequest>>()
            .Produces<ExtracurricularActivityDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/{activityId:guid}/participants", async (
                Guid activityId,
                EnrollExtracurricularParticipantRequest request,
                EnrollExtracurricularParticipantHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(activityId, request, ct);
                return result.IsSuccess
                    ? Results.Created($"/api/dev/extracurriculars/{activityId}/participants/{result.Value!.Id}", result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Development.Extracurricular.Manage)
            .AddEndpointFilter<ValidationFilter<EnrollExtracurricularParticipantRequest>>()
            .Produces<ExtracurricularParticipationDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/{activityId:guid}/participants", async (
                Guid activityId,
                [AsParameters] GetExtracurricularParticipantsQuery query,
                GetExtracurricularParticipantsHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(activityId, query, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(policy => policy.RequireAssertion(ctx =>
                ctx.User.Claims.Any(c => c.Type == "permissions" &&
                    (string.Equals(c.Value, Permissions.Development.Extracurricular.Manage, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(c.Value, Permissions.Development.Extracurricular.Log, StringComparison.OrdinalIgnoreCase)))))
            .Produces<IReadOnlyList<ExtracurricularParticipationDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{activityId:guid}/participants/{participationId:guid}/withdraw", async (
                Guid activityId,
                Guid participationId,
                WithdrawExtracurricularParticipantHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(activityId, participationId, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Development.Extracurricular.Manage)
            .Produces<ExtracurricularParticipationDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{activityId:guid}/participants/{participationId:guid}/complete", async (
                Guid activityId,
                Guid participationId,
                CompleteExtracurricularParticipantHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(activityId, participationId, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Development.Extracurricular.Manage)
            .Produces<ExtracurricularParticipationDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{activityId:guid}/participants/{participationId:guid}/log", async (
                Guid activityId,
                Guid participationId,
                RecordExtracurricularSessionLogRequest request,
                RecordExtracurricularSessionLogHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(activityId, participationId, request, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Development.Extracurricular.Log)
            .AddEndpointFilter<ValidationFilter<RecordExtracurricularSessionLogRequest>>()
            .Produces<ExtracurricularParticipationDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
