using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Std.Features.Admissions.RegisterCandidate;

public sealed class RegisterCandidateEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/std/candidates", async (
                RegisterCandidateRequest request,
                RegisterCandidateHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(request, ct);
                return result.IsSuccess
                    ? Results.Created($"/api/std/candidates/{result.Value!.Id}", result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Students - Admissions")
            .RequireAuthorization(Permissions.Students.Admissions.Register)
            .AddEndpointFilter<ValidationFilter<RegisterCandidateRequest>>()
            .Produces<CandidateDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
