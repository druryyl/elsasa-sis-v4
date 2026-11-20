using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using ElsasaSis.Contracts.Operational;
using ElsasaSis.Modules.Opr.Features.Timetables.GetPersonalStudentTimetable;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;
using ElsasaSis.SharedKernel.Security;

namespace ElsasaSis.UnitTests.Opr;

public sealed class GetPersonalStudentTimetableHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenCurrentUserHasNoStudentId_ReturnsForbidden()
    {
        var user = new TestCurrentUser { StudentId = null };
        var connFactory = new FakeSqlConnectionFactory(new RecordingDbState());
        var oprContract = new TestOperationalContract();

        var handler = new GetPersonalStudentTimetableHandler(user, connFactory, oprContract);
        var result = await handler.HandleAsync(new GetPersonalStudentTimetableQuery());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_WhenNoActivePeriod_ReturnsNotFound()
    {
        var studentId = Guid.NewGuid();
        var user = new TestCurrentUser { StudentId = studentId };
        var connFactory = new FakeSqlConnectionFactory(new RecordingDbState());
        var oprContract = new TestOperationalContract { ActivePeriod = null };

        var handler = new GetPersonalStudentTimetableHandler(user, connFactory, oprContract);
        var result = await handler.HandleAsync(new GetPersonalStudentTimetableQuery());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_WhenStudentNotFound_ReturnsNotFound()
    {
        var studentId = Guid.NewGuid();
        var user = new TestCurrentUser { StudentId = studentId };
        var dbState = new RecordingDbState();
        // Return empty table for student query
        dbState.EnqueueReaderResult(_ => true, CreateEmptyStudentTable());

        var connFactory = new FakeSqlConnectionFactory(dbState);
        var oprContract = new TestOperationalContract
        {
            ActivePeriod = new ActiveOperationalPeriodDto(
                Guid.NewGuid(), Guid.NewGuid(), "Semester 1",
                new DateOnly(2026, 7, 1), new DateOnly(2026, 12, 31))
        };

        var handler = new GetPersonalStudentTimetableHandler(user, connFactory, oprContract);
        var result = await handler.HandleAsync(new GetPersonalStudentTimetableQuery());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_WhenStudentUnplaced_ReturnsSuccessWithEmptyTimetable()
    {
        var studentId = Guid.NewGuid();
        var user = new TestCurrentUser { StudentId = studentId };
        var dbState = new RecordingDbState();

        // Student exists, but StudyGroupId is null
        var studentTable = CreateStudentTable(
            studentId, "Ahmad Dahlan", "1001", null, null, "SMK", "10");
        dbState.EnqueueReaderResult(_ => true, studentTable);

        var connFactory = new FakeSqlConnectionFactory(dbState);
        var oprContract = new TestOperationalContract
        {
            ActivePeriod = new ActiveOperationalPeriodDto(
                Guid.NewGuid(), Guid.NewGuid(), "Semester 1",
                new DateOnly(2026, 7, 1), new DateOnly(2026, 12, 31))
        };

        var handler = new GetPersonalStudentTimetableHandler(user, connFactory, oprContract);
        var result = await handler.HandleAsync(new GetPersonalStudentTimetableQuery());

        Assert.True(result.IsSuccess);
        var response = result.Value;
        Assert.NotNull(response);
        Assert.Equal(studentId, response.StudentInfo.StudentId);
        Assert.Equal("Ahmad Dahlan", response.StudentInfo.FullName);
        Assert.Null(response.StudentInfo.StudyGroupId);
        Assert.Equal("REGULAR", response.ActiveCycle.ScheduleType);
        Assert.False(response.ActiveCycle.IsSmkRotation);
        Assert.Empty(response.TodayAgenda);
        Assert.Empty(response.WeeklySchedule);
    }

    [Fact]
    public async Task HandleAsync_WhenVocationalBlockCycle_ResolvesVocationalAndRegularSlots()
    {
        var studentId = Guid.NewGuid();
        var studyGroupId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var user = new TestCurrentUser { StudentId = studentId };

        var dbState = new RecordingDbState();

        // 1. Student Placement query
        var studentTable = CreateStudentTable(
            studentId, "Ahmad Dahlan", "1001", studyGroupId, "X-TKJ-1", "SMK", "10");
        dbState.EnqueueReaderResult(sql => sql.Contains("std.students", StringComparison.OrdinalIgnoreCase), studentTable);

        // 2. SMK cycle query -> VOCATIONAL
        var cycleStartDate = new DateTime(2026, 11, 16);
        var cycleEndDate = new DateTime(2026, 11, 28);
        var cycleTable = CreateSmkCycleTable("VOCATIONAL", cycleStartDate, cycleEndDate);
        dbState.EnqueueReaderResult(sql => sql.Contains("opr.study_group_timetables", StringComparison.OrdinalIgnoreCase), cycleTable);

        // 3. Schedules query -> has GENERAL, VOCATIONAL, and REGULAR slots
        // Target date will be Monday, 2026-11-16 (DayOfWeek = 1)
        var schedulesTable = CreateSchedulesTable(
            (Guid.NewGuid(), 1, 1, new TimeSpan(7, 30, 0), new TimeSpan(8, 15, 0), Guid.NewGuid(), "Praktik Jaringan", "VOC-01", Guid.NewGuid(), "Pak Budi", null, null, "VOCATIONAL"),
            (Guid.NewGuid(), 1, 2, new TimeSpan(8, 15, 0), new TimeSpan(9, 0, 0), Guid.NewGuid(), "Praktik Komputer", "VOC-02", Guid.NewGuid(), "Pak Budi", null, null, "VOCATIONAL"),
            (Guid.NewGuid(), 1, 3, new TimeSpan(9, 15, 0), new TimeSpan(10, 0, 0), Guid.NewGuid(), "Upacara Bendera", "CER-01", Guid.NewGuid(), "Pak Kepala", null, null, "REGULAR"),
            (Guid.NewGuid(), 1, 4, new TimeSpan(10, 0, 0), new TimeSpan(10, 45, 0), Guid.NewGuid(), "Bahasa Indonesia", "GEN-01", Guid.NewGuid(), "Bu Siti", null, null, "GENERAL"), // should be filtered out!
            (Guid.NewGuid(), 2, 1, new TimeSpan(7, 30, 0), new TimeSpan(8, 15, 0), Guid.NewGuid(), "Praktik Jaringan 2", "VOC-03", Guid.NewGuid(), "Pak Budi", null, null, "VOCATIONAL")
        );
        dbState.EnqueueReaderResult(sql => sql.Contains("opr.study_group_schedules", StringComparison.OrdinalIgnoreCase), schedulesTable);

        var connFactory = new FakeSqlConnectionFactory(dbState);
        var oprContract = new TestOperationalContract
        {
            ActivePeriod = new ActiveOperationalPeriodDto(
                periodId, Guid.NewGuid(), "Semester 1",
                new DateOnly(2026, 7, 1), new DateOnly(2026, 12, 31))
        };

        var handler = new GetPersonalStudentTimetableHandler(user, connFactory, oprContract);
        var query = new GetPersonalStudentTimetableQuery(new DateOnly(2026, 11, 16)); // Monday
        var result = await handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        var response = result.Value;
        Assert.NotNull(response);

        // Check active cycle
        Assert.Equal("VOCATIONAL", response.ActiveCycle.ScheduleType);
        Assert.True(response.ActiveCycle.IsSmkRotation);
        Assert.Equal(new DateOnly(2026, 11, 16), response.ActiveCycle.StartDate);
        Assert.Equal(new DateOnly(2026, 11, 28), response.ActiveCycle.EndDate);

        // Check student info
        Assert.Equal(studentId, response.StudentInfo.StudentId);
        Assert.Equal("X-TKJ-1", response.StudentInfo.StudyGroupName);

        // Weekly schedule: excludes GENERAL slot (total 4: 3 on Monday, 1 on Tuesday)
        Assert.Equal(4, response.WeeklySchedule.Count);
        Assert.DoesNotContain(response.WeeklySchedule, s => s.ScheduleType == "GENERAL");
        Assert.Contains(response.WeeklySchedule, s => s.ScheduleType == "VOCATIONAL");
        Assert.Contains(response.WeeklySchedule, s => s.ScheduleType == "REGULAR");

        // Today agenda (Monday): 3 slots, ordered by period_slot 1, 2, 3
        Assert.Equal(3, response.TodayAgenda.Count);
        Assert.Equal(1, response.TodayAgenda[0].PeriodSlot);
        Assert.Equal("Praktik Jaringan", response.TodayAgenda[0].SubjectName);
        Assert.Equal("07:30", response.TodayAgenda[0].StartTime);
        Assert.Equal("Monday", response.TodayAgenda[0].DayOfWeek);

        Assert.Equal(2, response.TodayAgenda[1].PeriodSlot);
        Assert.Equal("Praktik Komputer", response.TodayAgenda[1].SubjectName);

        Assert.Equal(3, response.TodayAgenda[2].PeriodSlot);
        Assert.Equal("Upacara Bendera", response.TodayAgenda[2].SubjectName);
    }

    [Fact]
    public async Task HandleAsync_WhenGeneralBlockCycle_ResolvesGeneralAndRegularSlots()
    {
        var studentId = Guid.NewGuid();
        var studyGroupId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var user = new TestCurrentUser { StudentId = studentId };

        var dbState = new RecordingDbState();

        // 1. Student Placement query
        var studentTable = CreateStudentTable(
            studentId, "Ahmad Dahlan", "1001", studyGroupId, "X-TKJ-1", "SMK", "10");
        dbState.EnqueueReaderResult(sql => sql.Contains("std.students", StringComparison.OrdinalIgnoreCase), studentTable);

        // 2. SMK cycle query -> GENERAL
        var cycleStartDate = new DateTime(2026, 11, 2);
        var cycleEndDate = new DateTime(2026, 11, 14);
        var cycleTable = CreateSmkCycleTable("GENERAL", cycleStartDate, cycleEndDate);
        dbState.EnqueueReaderResult(sql => sql.Contains("opr.study_group_timetables", StringComparison.OrdinalIgnoreCase), cycleTable);

        // 3. Schedules query -> has GENERAL, VOCATIONAL, and REGULAR slots
        var schedulesTable = CreateSchedulesTable(
            (Guid.NewGuid(), 1, 1, new TimeSpan(7, 30, 0), new TimeSpan(8, 15, 0), Guid.NewGuid(), "Matematika", "MTK-01", Guid.NewGuid(), "Bu Nina", null, null, "GENERAL"),
            (Guid.NewGuid(), 1, 2, new TimeSpan(8, 15, 0), new TimeSpan(9, 0, 0), Guid.NewGuid(), "Bahasa Inggris", "ENG-01", Guid.NewGuid(), "Mr John", null, null, "GENERAL"),
            (Guid.NewGuid(), 1, 3, new TimeSpan(9, 15, 0), new TimeSpan(10, 0, 0), Guid.NewGuid(), "Upacara Bendera", "CER-01", Guid.NewGuid(), "Pak Kepala", null, null, "REGULAR"),
            (Guid.NewGuid(), 1, 4, new TimeSpan(10, 0, 0), new TimeSpan(10, 45, 0), Guid.NewGuid(), "Praktik Bengkel", "VOC-01", Guid.NewGuid(), "Pak Joko", null, null, "VOCATIONAL") // should be filtered out!
        );
        dbState.EnqueueReaderResult(sql => sql.Contains("opr.study_group_schedules", StringComparison.OrdinalIgnoreCase), schedulesTable);

        var connFactory = new FakeSqlConnectionFactory(dbState);
        var oprContract = new TestOperationalContract
        {
            ActivePeriod = new ActiveOperationalPeriodDto(
                periodId, Guid.NewGuid(), "Semester 1",
                new DateOnly(2026, 7, 1), new DateOnly(2026, 12, 31))
        };

        var handler = new GetPersonalStudentTimetableHandler(user, connFactory, oprContract);
        var query = new GetPersonalStudentTimetableQuery(new DateOnly(2026, 11, 2)); // Monday
        var result = await handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        var response = result.Value;
        Assert.NotNull(response);

        Assert.Equal("GENERAL", response.ActiveCycle.ScheduleType);
        Assert.True(response.ActiveCycle.IsSmkRotation);
        Assert.Equal(new DateOnly(2026, 11, 2), response.ActiveCycle.StartDate);
        Assert.Equal(new DateOnly(2026, 11, 14), response.ActiveCycle.EndDate);

        // Weekly schedule: excludes VOCATIONAL slot (total 3: 2 GENERAL + 1 REGULAR)
        Assert.Equal(3, response.WeeklySchedule.Count);
        Assert.DoesNotContain(response.WeeklySchedule, s => s.ScheduleType == "VOCATIONAL");
        Assert.Contains(response.WeeklySchedule, s => s.ScheduleType == "GENERAL");
        Assert.Contains(response.WeeklySchedule, s => s.ScheduleType == "REGULAR");

        // Today agenda
        Assert.Equal(3, response.TodayAgenda.Count);
        Assert.Equal("Matematika", response.TodayAgenda[0].SubjectName);
        Assert.Equal("Bahasa Inggris", response.TodayAgenda[1].SubjectName);
        Assert.Equal("Upacara Bendera", response.TodayAgenda[2].SubjectName);
    }

    [Fact]
    public async Task HandleAsync_WhenNoBlockCycleMatches_DefaultsToRegularNonSmk()
    {
        var studentId = Guid.NewGuid();
        var studyGroupId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var user = new TestCurrentUser { StudentId = studentId };

        var dbState = new RecordingDbState();

        // 1. Student Placement query
        var studentTable = CreateStudentTable(
            studentId, "Siti Nurhaliza", "1002", studyGroupId, "7A", "SMP", "7");
        dbState.EnqueueReaderResult(sql => sql.Contains("std.students", StringComparison.OrdinalIgnoreCase), studentTable);

        // 2. SMK cycle query -> empty (no match)
        var emptyCycleTable = CreateEmptySmkCycleTable();
        dbState.EnqueueReaderResult(sql => sql.Contains("opr.study_group_timetables", StringComparison.OrdinalIgnoreCase), emptyCycleTable);

        // 3. Schedules query -> has REGULAR slots
        var schedulesTable = CreateSchedulesTable(
            (Guid.NewGuid(), 2, 1, new TimeSpan(7, 30, 0), new TimeSpan(8, 15, 0), Guid.NewGuid(), "IPA", "IPA-01", Guid.NewGuid(), "Bu Ratna", null, null, "REGULAR"),
            (Guid.NewGuid(), 2, 2, new TimeSpan(8, 15, 0), new TimeSpan(9, 0, 0), Guid.NewGuid(), "IPS", "IPS-01", Guid.NewGuid(), "Pak Bambang", null, null, "REGULAR")
        );
        dbState.EnqueueReaderResult(sql => sql.Contains("opr.study_group_schedules", StringComparison.OrdinalIgnoreCase), schedulesTable);

        var connFactory = new FakeSqlConnectionFactory(dbState);
        var oprContract = new TestOperationalContract
        {
            ActivePeriod = new ActiveOperationalPeriodDto(
                periodId, Guid.NewGuid(), "Semester 1",
                new DateOnly(2026, 7, 1), new DateOnly(2026, 12, 31))
        };

        var handler = new GetPersonalStudentTimetableHandler(user, connFactory, oprContract);
        var query = new GetPersonalStudentTimetableQuery(new DateOnly(2026, 11, 17)); // Tuesday (Day 2)
        var result = await handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        var response = result.Value;
        Assert.NotNull(response);

        Assert.Equal("REGULAR", response.ActiveCycle.ScheduleType);
        Assert.False(response.ActiveCycle.IsSmkRotation);
        Assert.Null(response.ActiveCycle.StartDate);
        Assert.Null(response.ActiveCycle.EndDate);

        Assert.Equal(2, response.WeeklySchedule.Count);
        Assert.Equal(2, response.TodayAgenda.Count);
        Assert.Equal("Tuesday", response.TodayAgenda[0].DayOfWeek);
    }

    #region Helpers & Fakes

    private static DataTable CreateEmptyStudentTable()
    {
        var table = new DataTable();
        table.Columns.Add("StudentId", typeof(Guid));
        table.Columns.Add("FullName", typeof(string));
        table.Columns.Add("Nis", typeof(string));
        table.Columns.Add("StudyGroupId", typeof(Guid));
        table.Columns.Add("StudyGroupName", typeof(string));
        table.Columns.Add("SchoolLevel", typeof(string));
        table.Columns.Add("Grade", typeof(string));
        return table;
    }

    private static DataTable CreateStudentTable(
        Guid studentId, string fullName, string nis,
        Guid? studyGroupId, string? studyGroupName, string? schoolLevel, string? grade)
    {
        var table = CreateEmptyStudentTable();
        var row = table.NewRow();
        row["StudentId"] = studentId;
        row["FullName"] = fullName;
        row["Nis"] = nis;
        row["StudyGroupId"] = studyGroupId.HasValue ? studyGroupId.Value : DBNull.Value;
        row["StudyGroupName"] = studyGroupName ?? (object)DBNull.Value;
        row["SchoolLevel"] = schoolLevel ?? (object)DBNull.Value;
        row["Grade"] = grade ?? (object)DBNull.Value;
        table.Rows.Add(row);
        return table;
    }

    private static DataTable CreateEmptySmkCycleTable()
    {
        var table = new DataTable();
        table.Columns.Add("schedule_type", typeof(string));
        table.Columns.Add("start_date", typeof(DateTime));
        table.Columns.Add("end_date", typeof(DateTime));
        return table;
    }

    private static DataTable CreateSmkCycleTable(string scheduleType, DateTime startDate, DateTime endDate)
    {
        var table = CreateEmptySmkCycleTable();
        var row = table.NewRow();
        row["schedule_type"] = scheduleType;
        row["start_date"] = startDate;
        row["end_date"] = endDate;
        table.Rows.Add(row);
        return table;
    }

    private static DataTable CreateSchedulesTable(
        params (Guid Id, int DayOfWeek, int PeriodSlot, TimeSpan StartTime, TimeSpan EndTime,
                Guid SubjectId, string SubjectName, string SubjectCode, Guid TeacherId, string TeacherName,
                Guid? RoomId, string? RoomName, string ScheduleType)[] slots)
    {
        var table = new DataTable();
        table.Columns.Add("id", typeof(Guid));
        table.Columns.Add("day_of_week", typeof(int));
        table.Columns.Add("period_slot", typeof(int));
        table.Columns.Add("start_time", typeof(TimeSpan));
        table.Columns.Add("end_time", typeof(TimeSpan));
        table.Columns.Add("subject_id", typeof(Guid));
        table.Columns.Add("subject_name", typeof(string));
        table.Columns.Add("subject_code", typeof(string));
        table.Columns.Add("teacher_id", typeof(Guid));
        table.Columns.Add("teacher_name", typeof(string));
        table.Columns.Add("room_id", typeof(Guid));
        table.Columns.Add("room_name", typeof(string));
        table.Columns.Add("schedule_type", typeof(string));

        foreach (var slot in slots)
        {
            var row = table.NewRow();
            row["id"] = slot.Id;
            row["day_of_week"] = slot.DayOfWeek;
            row["period_slot"] = slot.PeriodSlot;
            row["start_time"] = slot.StartTime;
            row["end_time"] = slot.EndTime;
            row["subject_id"] = slot.SubjectId;
            row["subject_name"] = slot.SubjectName;
            row["subject_code"] = slot.SubjectCode;
            row["teacher_id"] = slot.TeacherId;
            row["teacher_name"] = slot.TeacherName;
            row["room_id"] = slot.RoomId.HasValue ? slot.RoomId.Value : DBNull.Value;
            row["room_name"] = slot.RoomName ?? (object)DBNull.Value;
            row["schedule_type"] = slot.ScheduleType;
            table.Rows.Add(row);
        }

        return table;
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid UserId => Guid.NewGuid();
        public string Username => "test.student";
        public string FullName => "Test Student";
        public IReadOnlyList<string> Roles => ["STUDENT"];
        public IReadOnlyList<string> Permissions => ["Permissions.Operations.Timetables.ViewOwn"];
        public Guid? TeacherId => null;
        public Guid? StudentId { get; set; } = Guid.NewGuid();
        public Guid? GuardianId => null;
    }

    private sealed class TestOperationalContract : IOperationalContract
    {
        public ActiveOperationalPeriodDto? ActivePeriod { get; set; }

        public Task<ActiveAcademicYearDto?> GetActiveAcademicYearAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<ActiveAcademicYearDto?>(null);

        public Task<ActiveOperationalPeriodDto?> GetActiveOperationalPeriodAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ActivePeriod);

        public Task<AcademicYearSummaryDto?> GetAcademicYearByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<AcademicYearSummaryDto?>(null);

        public Task<OperationalPeriodSummaryDto?> GetOperationalPeriodByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<OperationalPeriodSummaryDto?>(null);
    }

    private sealed class RecordingDbState
    {
        private readonly List<(Func<string, bool> Predicate, DataTable Table)> _readers = [];

        public void EnqueueReaderResult(Func<string, bool> predicate, DataTable table) =>
            _readers.Add((predicate, table));

        public DataTable ConsumeReader(string sql)
        {
            var idx = _readers.FindIndex(r => r.Predicate(sql));
            if (idx < 0)
            {
                return new DataTable();
            }

            var table = _readers[idx].Table;
            _readers.RemoveAt(idx);
            return table;
        }
    }

    private sealed class FakeSqlConnectionFactory(RecordingDbState state) : ISqlConnectionFactory
    {
        public IDbConnection CreateConnection() => new FakeDbConnection(state);
        public Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IDbConnection>(new FakeDbConnection(state));
    }

    private sealed class FakeDbConnection(RecordingDbState state) : DbConnection
    {
        private ConnectionState _connectionState = ConnectionState.Open;

        [AllowNull]
        public override string ConnectionString { get; set; } = string.Empty;
        public override string Database => "FakeDb";
        public override string DataSource => "FakeServer";
        public override string ServerVersion => "1.0";
        public override ConnectionState State => _connectionState;

        public override void ChangeDatabase(string databaseName) { }
        public override void Close() => _connectionState = ConnectionState.Closed;
        public override void Open() => _connectionState = ConnectionState.Open;

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
            new FakeDbTransaction(this);

        protected override DbCommand CreateDbCommand() => new FakeDbCommand(this, state);
    }

    private sealed class FakeDbTransaction(DbConnection connection) : DbTransaction
    {
        public override IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
        protected override DbConnection DbConnection => connection;
        public override void Commit() { }
        public override void Rollback() { }
    }

    private sealed class FakeDbCommand(DbConnection connection, RecordingDbState state) : DbCommand
    {
        private readonly FakeDbParameterCollection _parameters = new();

        [AllowNull]
        public override string CommandText { get; set; } = string.Empty;
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; } = connection;
        protected override DbParameterCollection DbParameterCollection => _parameters;
        protected override DbTransaction? DbTransaction { get; set; }

        public override void Cancel() { }
        public override int ExecuteNonQuery() => 1;
        public override object? ExecuteScalar() => null;
        public override void Prepare() { }
        protected override DbParameter CreateDbParameter() => new FakeDbParameter();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
            state.ConsumeReader(CommandText).CreateDataReader();
    }

    private sealed class FakeDbParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; }
        public override bool IsNullable { get; set; }
        [AllowNull]
        public override string ParameterName { get; set; } = string.Empty;
        [AllowNull]
        public override string SourceColumn { get; set; } = string.Empty;
        public override object? Value { get; set; }
        public override bool SourceColumnNullMapping { get; set; }
        public override int Size { get; set; }
        public override void ResetDbType() { }
    }

    private sealed class FakeDbParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _parameters = [];
        public override int Count => _parameters.Count;
        public override object SyncRoot => _parameters;
        public override int Add(object value)
        {
            _parameters.Add((DbParameter)value);
            return _parameters.Count - 1;
        }
        public override void AddRange(Array values) => _parameters.AddRange(values.Cast<DbParameter>());
        public override void Clear() => _parameters.Clear();
        public override bool Contains(object value) => _parameters.Contains((DbParameter)value);
        public override bool Contains(string value) => _parameters.Any(p => p.ParameterName == value);
        public override void CopyTo(Array array, int index) => ((System.Collections.ICollection)_parameters).CopyTo(array, index);
        public override System.Collections.IEnumerator GetEnumerator() => _parameters.GetEnumerator();
        public override int IndexOf(object value) => _parameters.IndexOf((DbParameter)value);
        public override int IndexOf(string parameterName) => _parameters.FindIndex(p => p.ParameterName == parameterName);
        public override void Insert(int index, object value) => _parameters.Insert(index, (DbParameter)value);
        public override void Remove(object value) => _parameters.Remove((DbParameter)value);
        public override void RemoveAt(int index) => _parameters.RemoveAt(index);
        public override void RemoveAt(string parameterName)
        {
            var idx = IndexOf(parameterName);
            if (idx >= 0) _parameters.RemoveAt(idx);
        }
        protected override DbParameter GetParameter(int index) => _parameters[index];
        protected override DbParameter GetParameter(string parameterName) => _parameters[IndexOf(parameterName)];
        protected override void SetParameter(int index, DbParameter value) => _parameters[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value) => _parameters[IndexOf(parameterName)] = value;
    }

    #endregion
}
