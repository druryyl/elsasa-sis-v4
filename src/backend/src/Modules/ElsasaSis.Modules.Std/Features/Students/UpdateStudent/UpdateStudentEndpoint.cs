using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Std.Features.Students.UpdateStudent;

public sealed class UpdateStudentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/std/students/{id:guid}", async (
                Guid id,
                UpdateStudentRequest request,
                UpdateStudentHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, request, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Students - Directory")
            .RequireAuthorization(Permissions.Students.Directory.Manage)
            .AddEndpointFilter<ValidationFilter<UpdateStudentRequest>>()
            .Produces<StudentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
