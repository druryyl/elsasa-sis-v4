using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Std.Features.BillingLookup;

public sealed class LookupStudentBillingContactsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/std/billing-lookup", async (
                [AsParameters] LookupStudentBillingContactsQuery query,
                GetStudentBillingContactsHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(query, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.BadRequest();
            })
            .WithTags("Students - Billing Contacts")
            .RequireAuthorization(Policies.Students.BillingLookup)
            .Produces<IReadOnlyList<StudentBillingContactDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
