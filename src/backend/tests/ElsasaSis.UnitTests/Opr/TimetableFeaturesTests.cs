using ElsasaSis.Contracts.Academic;
using ElsasaSis.Contracts.Operational;
using ElsasaSis.Contracts.Student;
using ElsasaSis.Contracts.Teaching;
using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.Modules.Opr.Domain.Model;
using ElsasaSis.Modules.Opr.Features.Timetables.ConfigureStudyGroupTimetable;
using ElsasaSis.Modules.Opr.Features.Timetables.GetTeacherPersonalSchedule;
using ElsasaSis.Modules.Opr.Features.Timetables.ManageSmkBlockRotations;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;
using ElsasaSis.SharedKernel.Security;

namespace ElsasaSis.UnitTests.Opr;

public sealed class TimetableFeaturesTests
{
    [Fact]
    public void ConfigureStudyGroupTimetableValidator_WhenEndTimeBeforeStartTime_Fails()
    {
        var validator = new ConfigureStudyGroupTimetableValidator();
        var request = new ConfigureStudyGroupTimetableRequest(
            Guid.NewGuid(),
            "REGULAR",
            1,
            1,
            new TimeOnly(8, 0),
            new TimeOnly(7, 0),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null);

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "EndTime");
    }

    [Fact]
    public void ConfigureStudyGroupTimetableValidator_WhenDayOfWeekInvalid_Fails()
    {
        var validator = new ConfigureStudyGroupTimetableValidator();
        var request = new ConfigureStudyGroupTimetableRequest(
            Guid.NewGuid(),
            "REGULAR",
            8,
            1,
            new TimeOnly(7, 0),
            new TimeOnly(7, 45),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null);

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "DayOfWeek");
    }

    [Fact]
    public void CreateSmkBlockValidator_WhenEndDateBeforeStartDate_Fails()
    {
        var validator = new CreateSmkBlockValidator();
        var request = new CreateSmkBlockRequest(
            Guid.NewGuid(),
            "GENERAL",
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 5));

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "EndDate");
    }

    [Fact]
    public void CreateSmkBlockValidator_WhenScheduleTypeIsRegular_Fails()
    {
        var validator = new CreateSmkBlockValidator();
        var request = new CreateSmkBlockRequest(
            Guid.NewGuid(),
            "REGULAR",
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 10));

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ScheduleType");
    }

    [Fact]
    public async Task ConfigureStudyGroupTimetableHandler_WhenPeriodNotFound_Returns404()
    {
        var scheduleRepo = new MockStudyGroupScheduleRepository();
        var oprContract = new MockOperationalContract { Period = null };
        var stdContract = new MockStudentContract();
        var acdContract = new MockAcademicContract();
        var tchContract = new MockTeachingContract();
        var roomRepo = new MockRoomRepository();

        var handler = new ConfigureStudyGroupTimetableHandler(
            scheduleRepo, oprContract, stdContract, acdContract, tchContract, roomRepo);

        var request = new ConfigureStudyGroupTimetableRequest(
            Guid.NewGuid(), "REGULAR", 1, 1, new TimeOnly(7, 0), new TimeOnly(7, 45),
            Guid.NewGuid(), Guid.NewGuid(), null);

        var result = await handler.HandleAsync(Guid.NewGuid(), request);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Opr.Timetable.OperationalPeriodNotFound", result.Error.Code);
    }

    [Fact]
    public async Task ConfigureStudyGroupTimetableHandler_WhenPeriodNotActive_Returns409()
    {
        var periodId = Guid.NewGuid();
        var scheduleRepo = new MockStudyGroupScheduleRepository();
        var oprContract = new MockOperationalContract
        {
            Period = new OperationalPeriodSummaryDto(
                periodId, Guid.NewGuid(), "2026/2027", "Semester 1",
                new DateOnly(2026, 7, 1), new DateOnly(2026, 12, 31), "INACTIVE")
        };
        var stdContract = new MockStudentContract();
        var acdContract = new MockAcademicContract();
        var tchContract = new MockTeachingContract();
        var roomRepo = new MockRoomRepository();

        var handler = new ConfigureStudyGroupTimetableHandler(
            scheduleRepo, oprContract, stdContract, acdContract, tchContract, roomRepo);

        var request = new ConfigureStudyGroupTimetableRequest(
            periodId, "REGULAR", 1, 1, new TimeOnly(7, 0), new TimeOnly(7, 45),
            Guid.NewGuid(), Guid.NewGuid(), null);

        var result = await handler.HandleAsync(Guid.NewGuid(), request);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Opr.Timetable.OperationalPeriodNotActive", result.Error.Code);
    }

    [Fact]
    public async Task ConfigureStudyGroupTimetableHandler_WhenStudyGroupNotFound_Returns404()
    {
        var periodId = Guid.NewGuid();
        var scheduleRepo = new MockStudyGroupScheduleRepository();
        var oprContract = new MockOperationalContract
        {
            Period = new OperationalPeriodSummaryDto(
                periodId, Guid.NewGuid(), "2026/2027", "Semester 1",
                new DateOnly(2026, 7, 1), new DateOnly(2026, 12, 31), "ACTIVE")
        };
        var stdContract = new MockStudentContract { StudyGroup = null };
        var acdContract = new MockAcademicContract();
        var tchContract = new MockTeachingContract();
        var roomRepo = new MockRoomRepository();

        var handler = new ConfigureStudyGroupTimetableHandler(
            scheduleRepo, oprContract, stdContract, acdContract, tchContract, roomRepo);

        var request = new ConfigureStudyGroupTimetableRequest(
            periodId, "REGULAR", 1, 1, new TimeOnly(7, 0), new TimeOnly(7, 45),
            Guid.NewGuid(), Guid.NewGuid(), null);

        var result = await handler.HandleAsync(Guid.NewGuid(), request);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Opr.Timetable.StudyGroupNotFound", result.Error.Code);
    }

    [Fact]
    public async Task ConfigureStudyGroupTimetableHandler_WhenSubjectNotFound_Returns404()
    {
        var periodId = Guid.NewGuid();
        var studyGroupId = Guid.NewGuid();
        var scheduleRepo = new MockStudyGroupScheduleRepository();
        var oprContract = new MockOperationalContract
        {
            Period = new OperationalPeriodSummaryDto(
                periodId, Guid.NewGuid(), "2026/2027", "Semester 1",
                new DateOnly(2026, 7, 1), new DateOnly(2026, 12, 31), "ACTIVE")
        };
        var stdContract = new MockStudentContract
        {
            StudyGroup = new StudyGroupSummaryDto(studyGroupId, Guid.NewGuid(), "2026/2027", "SMP", "7", "7A", null, null)
        };
        var acdContract = new MockAcademicContract { Subject = null };
        var tchContract = new MockTeachingContract();
        var roomRepo = new MockRoomRepository();

        var handler = new ConfigureStudyGroupTimetableHandler(
            scheduleRepo, oprContract, stdContract, acdContract, tchContract, roomRepo);

        var request = new ConfigureStudyGroupTimetableRequest(
            periodId, "REGULAR", 1, 1, new TimeOnly(7, 0), new TimeOnly(7, 45),
            Guid.NewGuid(), Guid.NewGuid(), null);

        var result = await handler.HandleAsync(studyGroupId, request);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Opr.Timetable.SubjectNotFound", result.Error.Code);
    }

    [Fact]
    public async Task ConfigureStudyGroupTimetableHandler_WhenTeacherNotFound_Returns404()
    {
        var periodId = Guid.NewGuid();
        var studyGroupId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var scheduleRepo = new MockStudyGroupScheduleRepository();
        var oprContract = new MockOperationalContract
        {
            Period = new OperationalPeriodSummaryDto(
                periodId, Guid.NewGuid(), "2026/2027", "Semester 1",
                new DateOnly(2026, 7, 1), new DateOnly(2026, 12, 31), "ACTIVE")
        };
        var stdContract = new MockStudentContract
        {
            StudyGroup = new StudyGroupSummaryDto(studyGroupId, Guid.NewGuid(), "2026/2027", "SMP", "7", "7A", null, null)
        };
        var acdContract = new MockAcademicContract
        {
            Subject = new SubjectDto(subjectId, "Matematika", "MTK")
        };
        var tchContract = new MockTeachingContract { Teacher = null };
        var roomRepo = new MockRoomRepository();

        var handler = new ConfigureStudyGroupTimetableHandler(
            scheduleRepo, oprContract, stdContract, acdContract, tchContract, roomRepo);

        var request = new ConfigureStudyGroupTimetableRequest(
            periodId, "REGULAR", 1, 1, new TimeOnly(7, 0), new TimeOnly(7, 45),
            subjectId, Guid.NewGuid(), null);

        var result = await handler.HandleAsync(studyGroupId, request);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Opr.Timetable.TeacherNotFound", result.Error.Code);
    }

    [Fact]
    public async Task ConfigureStudyGroupTimetableHandler_WhenRoomProvidedAndNotFound_Returns404()
    {
        var periodId = Guid.NewGuid();
        var studyGroupId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var roomId = Guid.NewGuid();

        var scheduleRepo = new MockStudyGroupScheduleRepository();
        var oprContract = new MockOperationalContract
        {
            Period = new OperationalPeriodSummaryDto(
                periodId, Guid.NewGuid(), "2026/2027", "Semester 1",
                new DateOnly(2026, 7, 1), new DateOnly(2026, 12, 31), "ACTIVE")
        };
        var stdContract = new MockStudentContract
        {
            StudyGroup = new StudyGroupSummaryDto(studyGroupId, Guid.NewGuid(), "2026/2027", "SMP", "7", "7A", null, null)
        };
        var acdContract = new MockAcademicContract
        {
            Subject = new SubjectDto(subjectId, "Matematika", "MTK")
        };
        var tchContract = new MockTeachingContract
        {
            Teacher = new TeacherSummaryDto(teacherId, "Budi Santoso", "ACTIVE")
        };
        var roomRepo = new MockRoomRepository { Room = null };

        var handler = new ConfigureStudyGroupTimetableHandler(
            scheduleRepo, oprContract, stdContract, acdContract, tchContract, roomRepo);

        var request = new ConfigureStudyGroupTimetableRequest(
            periodId, "REGULAR", 1, 1, new TimeOnly(7, 0), new TimeOnly(7, 45),
            subjectId, teacherId, roomId);

        var result = await handler.HandleAsync(studyGroupId, request);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Opr.Timetable.RoomNotFound", result.Error.Code);
    }

    [Fact]
    public async Task ConfigureStudyGroupTimetableHandler_WhenNewSlot_AddsAndReturnsDto()
    {
        var periodId = Guid.NewGuid();
        var studyGroupId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var roomId = Guid.NewGuid();

        var scheduleRepo = new MockStudyGroupScheduleRepository();
        var oprContract = new MockOperationalContract
        {
            Period = new OperationalPeriodSummaryDto(
                periodId, Guid.NewGuid(), "2026/2027", "Semester 1",
                new DateOnly(2026, 7, 1), new DateOnly(2026, 12, 31), "ACTIVE")
        };
        var stdContract = new MockStudentContract
        {
            StudyGroup = new StudyGroupSummaryDto(studyGroupId, Guid.NewGuid(), "2026/2027", "SMP", "7", "7A", null, null)
        };
        var acdContract = new MockAcademicContract
        {
            Subject = new SubjectDto(subjectId, "Matematika", "MTK")
        };
        var tchContract = new MockTeachingContract
        {
            Teacher = new TeacherSummaryDto(teacherId, "Budi Santoso", "ACTIVE")
        };
        var room = Room.Create("Room 101", RoomType.Classroom, 30).Value!;
        var roomRepo = new MockRoomRepository { Room = room };

        var handler = new ConfigureStudyGroupTimetableHandler(
            scheduleRepo, oprContract, stdContract, acdContract, tchContract, roomRepo);

        var request = new ConfigureStudyGroupTimetableRequest(
            periodId, "REGULAR", 1, 1, new TimeOnly(7, 0), new TimeOnly(7, 45),
            subjectId, teacherId, room.Id);

        var result = await handler.HandleAsync(studyGroupId, request);

        Assert.True(result.IsSuccess);
        var dto = result.Value!;
        Assert.Equal("7A", dto.StudyGroupName);
        Assert.Equal("Matematika", dto.SubjectName);
        Assert.Equal("Budi Santoso", dto.TeacherName);
        Assert.Equal(room.Name, dto.RoomName);
        Assert.Single(scheduleRepo.Schedules);
    }

    [Fact]
    public async Task DeleteStudyGroupTimetableSlotHandler_WhenNotFound_Returns404()
    {
        var scheduleRepo = new MockStudyGroupScheduleRepository();
        var handler = new DeleteStudyGroupTimetableSlotHandler(scheduleRepo);

        var result = await handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task DeleteStudyGroupTimetableSlotHandler_WhenValid_Deletes()
    {
        var studyGroupId = Guid.NewGuid();
        var schedule = StudyGroupSchedule.Create(
            Guid.NewGuid(), studyGroupId, "7A", "REGULAR", 1, 1,
            new TimeOnly(7, 0), new TimeOnly(7, 45),
            Guid.NewGuid(), "MTK", Guid.NewGuid(), "Teacher").Value!;

        var scheduleRepo = new MockStudyGroupScheduleRepository { Schedule = schedule };
        scheduleRepo.Schedules.Add(schedule);

        var handler = new DeleteStudyGroupTimetableSlotHandler(scheduleRepo);

        var result = await handler.HandleAsync(studyGroupId, schedule.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(scheduleRepo.Schedules);
    }

    [Fact]
    public async Task CreateSmkBlockHandler_WhenValid_AddsBlock()
    {
        var periodId = Guid.NewGuid();
        var studyGroupId = Guid.NewGuid();

        var timetableRepo = new MockStudyGroupTimetableRepository();
        var oprContract = new MockOperationalContract
        {
            Period = new OperationalPeriodSummaryDto(
                periodId, Guid.NewGuid(), "2026/2027", "Semester 1",
                new DateOnly(2026, 7, 1), new DateOnly(2026, 12, 31), "ACTIVE")
        };
        var stdContract = new MockStudentContract
        {
            StudyGroup = new StudyGroupSummaryDto(studyGroupId, Guid.NewGuid(), "2026/2027", "SMK", "10", "10 TKJ", null, null)
        };

        var handler = new CreateSmkBlockHandler(timetableRepo, oprContract, stdContract);

        var request = new CreateSmkBlockRequest(
            periodId, "GENERAL", new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 14));

        var result = await handler.HandleAsync(studyGroupId, request);

        Assert.True(result.IsSuccess);
        var dto = result.Value!;
        Assert.Equal("10 TKJ", dto.StudyGroupName);
        Assert.Equal("GENERAL", dto.ScheduleType);
        Assert.Single(timetableRepo.Timetables);
    }

    [Fact]
    public async Task DeleteSmkBlockHandler_WhenValid_DeletesBlock()
    {
        var studyGroupId = Guid.NewGuid();
        var block = StudyGroupTimetable.Create(
            Guid.NewGuid(), studyGroupId, "10 TKJ", "GENERAL",
            new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 14)).Value!;

        var timetableRepo = new MockStudyGroupTimetableRepository { Timetable = block };
        timetableRepo.Timetables.Add(block);

        var handler = new DeleteSmkBlockHandler(timetableRepo);

        var result = await handler.HandleAsync(studyGroupId, block.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(timetableRepo.Timetables);
    }

    [Fact]
    public async Task GetTeacherPersonalScheduleHandler_WhenNoTeacherId_ReturnsForbidden()
    {
        var currentUser = new MockCurrentUser { TeacherId = null };
        var connFactory = new MockSqlConnectionFactory();
        var handler = new GetTeacherPersonalScheduleHandler(currentUser, connFactory);

        var result = await handler.HandleAsync(new GetTeacherPersonalScheduleQuery(Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        Assert.Equal("Opr.Timetable.TeacherNotAssignedToUser", result.Error.Code);
    }

    private sealed class MockStudyGroupScheduleRepository : IStudyGroupScheduleRepository
    {
        public List<StudyGroupSchedule> Schedules { get; } = [];
        public StudyGroupSchedule? Schedule { get; set; }

        public Task<StudyGroupSchedule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Schedules.FirstOrDefault(s => s.Id == id) ?? Schedule);
        }

        public Task<StudyGroupSchedule?> GetBySlotAsync(
            Guid operationalPeriodId, Guid studyGroupId, string scheduleType, int dayOfWeek, int periodSlot, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Schedules.FirstOrDefault(s =>
                s.OperationalPeriodId == operationalPeriodId &&
                s.StudyGroupId == studyGroupId &&
                s.ScheduleType == scheduleType &&
                s.DayOfWeek == dayOfWeek &&
                s.PeriodSlot == periodSlot));
        }

        public Task AddAsync(StudyGroupSchedule schedule, CancellationToken cancellationToken = default)
        {
            Schedules.Add(schedule);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(StudyGroupSchedule schedule, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task UpsertAsync(StudyGroupSchedule schedule, CancellationToken cancellationToken = default)
        {
            var idx = Schedules.FindIndex(s => s.Id == schedule.Id);
            if (idx >= 0) Schedules[idx] = schedule;
            else Schedules.Add(schedule);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Schedules.RemoveAll(s => s.Id == id);
            return Task.CompletedTask;
        }
    }

    private sealed class MockStudyGroupTimetableRepository : IStudyGroupTimetableRepository
    {
        public List<StudyGroupTimetable> Timetables { get; } = [];
        public StudyGroupTimetable? Timetable { get; set; }

        public Task<StudyGroupTimetable?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Timetables.FirstOrDefault(t => t.Id == id) ?? Timetable);
        }

        public Task<StudyGroupTimetable?> GetByPeriodGroupDateAsync(
            Guid operationalPeriodId, Guid studyGroupId, DateOnly startDate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Timetables.FirstOrDefault(t =>
                t.OperationalPeriodId == operationalPeriodId &&
                t.StudyGroupId == studyGroupId &&
                t.StartDate == startDate));
        }

        public Task<IReadOnlyList<StudyGroupTimetable>> GetByStudyGroupIdAsync(
            Guid studyGroupId, Guid operationalPeriodId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<StudyGroupTimetable>>(
                Timetables.Where(t => t.StudyGroupId == studyGroupId && t.OperationalPeriodId == operationalPeriodId).ToList());
        }

        public Task AddAsync(StudyGroupTimetable timetable, CancellationToken cancellationToken = default)
        {
            Timetables.Add(timetable);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(StudyGroupTimetable timetable, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task UpsertAsync(StudyGroupTimetable timetable, CancellationToken cancellationToken = default)
        {
            var idx = Timetables.FindIndex(t => t.Id == timetable.Id);
            if (idx >= 0) Timetables[idx] = timetable;
            else Timetables.Add(timetable);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Timetables.RemoveAll(t => t.Id == id);
            return Task.CompletedTask;
        }
    }

    private sealed class MockOperationalContract : IOperationalContract
    {
        public OperationalPeriodSummaryDto? Period { get; set; }

        public Task<ActiveAcademicYearDto?> GetActiveAcademicYearAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<ActiveAcademicYearDto?>(null);

        public Task<ActiveOperationalPeriodDto?> GetActiveOperationalPeriodAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<ActiveOperationalPeriodDto?>(null);

        public Task<AcademicYearSummaryDto?> GetAcademicYearByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<AcademicYearSummaryDto?>(null);

        public Task<OperationalPeriodSummaryDto?> GetOperationalPeriodByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Period);
    }

    private sealed class MockStudentContract : IStudentContract
    {
        public StudyGroupSummaryDto? StudyGroup { get; set; }

        public Task<StudyGroupSummaryDto?> GetStudyGroupByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(StudyGroup);

        public Task<IReadOnlyList<StudentPlacementScopeDto>> GetActiveStudentsByScopeAsync(
            Guid? studyGroupId,
            string? schoolLevel,
            string? grade,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StudentPlacementScopeDto>>([]);
    }

    private sealed class MockAcademicContract : IAcademicContract
    {
        public SubjectDto? Subject { get; set; }

        public Task<IReadOnlyList<SubjectDto>> GetAllSubjectsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SubjectDto>>([]);

        public Task<SubjectDto?> GetSubjectByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Subject);
    }

    private sealed class MockTeachingContract : ITeachingContract
    {
        public TeacherSummaryDto? Teacher { get; set; }

        public Task<TeacherSummaryDto?> GetTeacherByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Teacher);
    }

    private sealed class MockRoomRepository : IRoomRepository
    {
        public Room? Room { get; set; }

        public Task<Room?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Room);

        public Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<Result> AddAsync(Room room, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());

        public Task<Result> UpdateAsync(Room room, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());
    }

    private sealed class MockCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid UserId => Guid.NewGuid();
        public string Username => "test.teacher";
        public string FullName => "Test Teacher";
        public IReadOnlyList<string> Roles => ["TEACHER"];
        public IReadOnlyList<string> Permissions => ["Permissions.Operations.Timetables.ViewOwn"];
        public Guid? TeacherId { get; set; }
        public Guid? StudentId => null;
        public Guid? GuardianId => null;
    }

    private sealed class MockSqlConnectionFactory : ISqlConnectionFactory
    {
        public System.Data.IDbConnection CreateConnection() => throw new NotImplementedException();
        public Task<System.Data.IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
