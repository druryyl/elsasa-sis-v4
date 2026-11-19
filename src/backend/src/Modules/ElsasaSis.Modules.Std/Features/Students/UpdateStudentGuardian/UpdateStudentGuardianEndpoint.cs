using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Std.Features.Students.UpdateStudentGuardian;

public sealed class UpdateStudentGuardianEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/std/students/{id:guid}/guardians/{guardianId:guid}", async (
                Guid id,
                Guid guardianId,
                UpdateStudentGuardianRequest request,
                UpdateStudentGuardianHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, guardianId, request, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Students - Guardians")
            .RequireAuthorization(Permissions.Students.Directory.Manage)
            .AddEndpointFilter<ValidationFilter<UpdateStudentGuardianRequest>>()
            .Produces<StudentGuardianDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
