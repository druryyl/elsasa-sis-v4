using ElsasaSis.Modules.Opr.Features.AcademicYears;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Opr.Features.Announcements.UpdateAnnouncement;

public sealed class UpdateAnnouncementEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/opr/announcements/{id:guid}", async (
                Guid id,
                UpdateAnnouncementRequest request,
                UpdateAnnouncementHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, request, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Operations - Announcements")
            .RequireAuthorization(Permissions.Operations.Announcements.Create)
            .AddEndpointFilter<ValidationFilter<UpdateAnnouncementRequest>>()
            .Produces<AnnouncementDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
