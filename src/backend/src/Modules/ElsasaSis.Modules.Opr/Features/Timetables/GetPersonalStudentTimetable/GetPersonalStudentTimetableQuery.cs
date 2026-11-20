using ElsasaSis.SharedKernel.Common;
using MediatR;

namespace ElsasaSis.Modules.Opr.Features.Timetables.GetPersonalStudentTimetable;

public sealed record GetPersonalStudentTimetableQuery(DateOnly? Date = null)
    : IRequest<Result<StudentPersonalScheduleResponse>>;
