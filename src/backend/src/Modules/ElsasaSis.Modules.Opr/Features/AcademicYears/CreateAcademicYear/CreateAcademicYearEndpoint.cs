using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Opr.Features.AcademicYears.CreateAcademicYear;

public sealed class CreateAcademicYearEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/opr/academic-years", async (
                CreateAcademicYearRequest request,
                CreateAcademicYearHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(request, ct);
                return result.IsSuccess
                    ? Results.Created($"/api/opr/academic-years/{result.Value!.Id}", result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Operations - Academic Years")
            .RequireAuthorization(Permissions.Operations.Calendar.Manage)
            .AddEndpointFilter<ValidationFilter<CreateAcademicYearRequest>>()
            .Produces<AcademicYearDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
