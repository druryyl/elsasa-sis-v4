using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Acd.Features.Attendance.GetSubjectAttendanceSheet;

public sealed class GetSubjectAttendanceSheetEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/acd/attendance", async (
                [FromQuery] Guid subjectId,
                [FromQuery] Guid studyGroupId,
                [FromQuery] DateOnly attendanceDate,
                [FromQuery] Guid operationalPeriodId,
                GetSubjectAttendanceSheetHandler handler,
                CancellationToken ct) =>
            {
                var query = new GetSubjectAttendanceSheetQuery(subjectId, studyGroupId, attendanceDate, operationalPeriodId);
                var result = await handler.HandleAsync(query, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Academic - Attendance")
            .RequireAuthorization(Policies.Academic.Attendance.ViewOrRecord)
            .Produces<SubjectAttendanceSheetResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
