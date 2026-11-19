using ElsasaSis.Modules.Std.Features.Students;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Std.Features.Placements.PlaceStudent;

public sealed class PlaceStudentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/std/students/{id:guid}/placements", async (
                Guid id,
                PlaceStudentRequest request,
                PlaceStudentHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, request, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Students - Placements")
            .RequireAuthorization(Permissions.Students.Placements.Manage)
            .AddEndpointFilter<ValidationFilter<PlaceStudentRequest>>()
            .Produces<StudentPlacementDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        app.MapPost("/api/std/placements", async (
                PlaceStudentFlatRequest request,
                PlaceStudentHandler handler,
                CancellationToken ct) =>
            {
                var nestedRequest = new PlaceStudentRequest(
                    request.AcademicYearId,
                    request.SchoolLevel,
                    request.Grade,
                    request.StudyGroupId);

                var result = await handler.HandleAsync(request.StudentId, nestedRequest, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Students - Placements")
            .RequireAuthorization(Permissions.Students.Placements.Manage)
            .AddEndpointFilter<ValidationFilter<PlaceStudentFlatRequest>>()
            .Produces<StudentPlacementDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
