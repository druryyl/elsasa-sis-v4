using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Std.Features.Students.AddStudentGuardian;

public sealed class AddStudentGuardianEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/std/students/{id:guid}/guardians", async (
                Guid id,
                AddStudentGuardianRequest request,
                AddStudentGuardianHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, request, ct);
                return result.IsSuccess
                    ? Results.Created($"/api/std/students/{id}/guardians/{result.Value!.Id}", result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Students - Guardians")
            .RequireAuthorization(Permissions.Students.Directory.Manage)
            .AddEndpointFilter<ValidationFilter<AddStudentGuardianRequest>>()
            .Produces<StudentGuardianDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
