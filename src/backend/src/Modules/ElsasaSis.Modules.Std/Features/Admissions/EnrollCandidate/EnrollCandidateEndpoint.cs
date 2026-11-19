using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Std.Features.Admissions.EnrollCandidate;

public sealed class EnrollCandidateEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/std/candidates/{id:guid}/enroll", async (
                Guid id,
                EnrollCandidateRequest request,
                EnrollCandidateHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, request, ct);
                return result.IsSuccess
                    ? Results.Created($"/api/std/students/{result.Value!.Id}", result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Students - Admissions")
            .RequireAuthorization(Permissions.Students.Admissions.Enroll)
            .AddEndpointFilter<ValidationFilter<EnrollCandidateRequest>>()
            .Produces<EnrolledStudentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
