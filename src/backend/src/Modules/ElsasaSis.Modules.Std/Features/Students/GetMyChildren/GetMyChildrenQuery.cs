using ElsasaSis.SharedKernel.Common;
using MediatR;

namespace ElsasaSis.Modules.Std.Features.Students.GetMyChildren;

public sealed record GetMyChildrenQuery : IRequest<Result<IReadOnlyList<LinkedChildDto>>>;
