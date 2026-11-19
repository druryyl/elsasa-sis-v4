using ElsasaSis.Contracts.Operational;
using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.Modules.Opr.Features.AcademicYears.ActivateAcademicYear;
using ElsasaSis.Modules.Opr.Features.AcademicYears.ActivateOperationalPeriod;
using ElsasaSis.Modules.Opr.Features.AcademicYears.CreateAcademicYear;
using ElsasaSis.Modules.Opr.Features.AcademicYears.GetAcademicYearById;
using ElsasaSis.Modules.Opr.Features.AcademicYears.GetAcademicYears;
using ElsasaSis.Modules.Opr.Features.AcademicYears.UpdateAcademicYear;
using ElsasaSis.Modules.Opr.Features.Announcements.CreateAnnouncement;
using ElsasaSis.Modules.Opr.Features.Announcements.GetAnnouncementById;
using ElsasaSis.Modules.Opr.Features.Announcements.GetAnnouncements;
using ElsasaSis.Modules.Opr.Features.Announcements.PublishAnnouncement;
using ElsasaSis.Modules.Opr.Features.Announcements.UpdateAnnouncement;
using ElsasaSis.Modules.Opr.Features.CalendarEntries.CreateCalendarEntry;
using ElsasaSis.Modules.Opr.Features.CalendarEntries.DeleteCalendarEntry;
using ElsasaSis.Modules.Opr.Features.CalendarEntries.GetCalendarEntries;
using ElsasaSis.Modules.Opr.Features.CalendarEntries.UpdateCalendarEntry;
using ElsasaSis.Modules.Opr.Features.Rooms.CreateRoom;
using ElsasaSis.Modules.Opr.Features.Rooms.GetRooms;
using ElsasaSis.Modules.Opr.Features.Rooms.UpdateRoom;
using ElsasaSis.Modules.Opr.Infrastructure.Contracts;
using ElsasaSis.Modules.Opr.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ElsasaSis.Modules.Opr;

public static class DependencyInjection
{
    public static IServiceCollection AddOprModule(this IServiceCollection services)
    {
        services.AddScoped<IAcademicYearRepository, AcademicYearRepository>();
        services.AddScoped<ICalendarEntryRepository, CalendarEntryRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
        services.AddScoped<IOperationalContract, OperationalContract>();

        services.AddScoped<GetAcademicYearsHandler>();
        services.AddScoped<GetAcademicYearByIdHandler>();
        services.AddScoped<CreateAcademicYearHandler>();
        services.AddScoped<UpdateAcademicYearHandler>();
        services.AddScoped<ActivateAcademicYearHandler>();
        services.AddScoped<ActivateOperationalPeriodHandler>();

        services.AddScoped<GetCalendarEntriesHandler>();
        services.AddScoped<CreateCalendarEntryHandler>();
        services.AddScoped<UpdateCalendarEntryHandler>();
        services.AddScoped<DeleteCalendarEntryHandler>();

        services.AddScoped<GetRoomsHandler>();
        services.AddScoped<CreateRoomHandler>();
        services.AddScoped<UpdateRoomHandler>();

        services.AddScoped<GetAnnouncementsHandler>();
        services.AddScoped<GetAnnouncementByIdHandler>();
        services.AddScoped<CreateAnnouncementHandler>();
        services.AddScoped<UpdateAnnouncementHandler>();
        services.AddScoped<PublishAnnouncementHandler>();

        services.AddScoped<IValidator<CreateAcademicYearRequest>, CreateAcademicYearValidator>();
        services.AddScoped<IValidator<UpdateAcademicYearRequest>, UpdateAcademicYearValidator>();
        services.AddScoped<IValidator<CreateCalendarEntryRequest>, CreateCalendarEntryValidator>();
        services.AddScoped<IValidator<UpdateCalendarEntryRequest>, UpdateCalendarEntryValidator>();
        services.AddScoped<IValidator<CreateRoomRequest>, CreateRoomValidator>();
        services.AddScoped<IValidator<UpdateRoomRequest>, UpdateRoomValidator>();
        services.AddScoped<IValidator<CreateAnnouncementRequest>, CreateAnnouncementValidator>();
        services.AddScoped<IValidator<UpdateAnnouncementRequest>, UpdateAnnouncementValidator>();

        return services;
    }
}
