using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Opr.Features.AcademicYears.UpdateAcademicYear;

public sealed class UpdateAcademicYearEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/opr/academic-years/{id:guid}", async (
                Guid id,
                UpdateAcademicYearRequest request,
                UpdateAcademicYearHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, request, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Operations - Academic Years")
            .RequireAuthorization(Permissions.Operations.Calendar.Manage)
            .AddEndpointFilter<ValidationFilter<UpdateAcademicYearRequest>>()
            .Produces<AcademicYearDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
