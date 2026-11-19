using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.SharedKernel.Endpoints;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
