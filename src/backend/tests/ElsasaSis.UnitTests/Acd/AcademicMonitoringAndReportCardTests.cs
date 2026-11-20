using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using ElsasaSis.Modules.Acd;
using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.Modules.Acd.Features.Monitoring.GetAcademicMonitoringProgress;
using ElsasaSis.Modules.Acd.Features.ReportCards.GetReportCardById;
using ElsasaSis.Modules.Acd.Features.ReportCards.GetStudentReportCards;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;
using ElsasaSis.SharedKernel.Security;
using Microsoft.Extensions.DependencyInjection;

namespace ElsasaSis.UnitTests.Acd;

public sealed class AcademicMonitoringAndReportCardTests
{
    private static readonly Guid ParentUserId = Guid.NewGuid();
    private static readonly Guid StaffUserId = Guid.NewGuid();
    private static readonly Guid LinkedStudentId = Guid.NewGuid();
    private static readonly Guid OtherStudentId = Guid.NewGuid();
    private static readonly Guid AcademicYearId = Guid.NewGuid();
    private static readonly Guid OperationalPeriodId = Guid.NewGuid();
    private static readonly Guid StudyGroup1Id = Guid.NewGuid();
    private static readonly Guid StudyGroup2Id = Guid.NewGuid();
    private static readonly Guid Assignment1Id = Guid.NewGuid();
    private static readonly Guid Assignment2Id = Guid.NewGuid();
    private static readonly Guid DraftCardId = Guid.NewGuid();
    private static readonly Guid FinalizedCardId = Guid.NewGuid();

    [Fact]
    public async Task GetAcademicMonitoringProgressHandler_ComputesKpisAndAlertsAccurately()
    {
        var targetDate = new DateOnly(2026, 11, 20);
        var periodEndDate = new DateTime(2026, 11, 30); // 10 days away -> within 14 days!

        var connection = new FakeDbConnection
        {
            OnExecuteReader = (sql, p) =>
            {
                if (sql.Contains("FROM std.study_groups", StringComparison.OrdinalIgnoreCase))
                {
                    return new FakeDbDataReader(
                        ["StudyGroupId", "StudyGroupName", "SchoolLevel", "Grade", "HomeroomTeacherName"],
                        [typeof(Guid), typeof(string), typeof(string), typeof(string), typeof(string)],
                        [
                            [StudyGroup1Id, "7A", "SMP", "7", "Budi Santoso"],
                            [StudyGroup2Id, "7B", "SMP", "7", "Siti Rahma"]
                        ]);
                }

                if (sql.Contains("FROM std.student_placements", StringComparison.OrdinalIgnoreCase))
                {
                    return new FakeDbDataReader(
                        ["StudyGroupId", "EnrolledCount"],
                        [typeof(Guid), typeof(int)],
                        [
                            [StudyGroup1Id, 20],
                            [StudyGroup2Id, 20]
                        ]);
                }

                if (sql.Contains("FROM acd.attendance_records", StringComparison.OrdinalIgnoreCase))
                {
                    // StudyGroup1 has attendance recorded today: 18 present, 2 late -> 20 attended.
                    // StudyGroup2 has NO attendance records today (missing attendance alert!).
                    return new FakeDbDataReader(
                        ["StudyGroupId", "PresentCount", "AbsentCount", "SickCount", "LateCount", "ExcusedCount", "TotalCount"],
                        [typeof(Guid), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int)],
                        [
                            [StudyGroup1Id, 18, 0, 0, 2, 0, 20]
                        ]);
                }

                if (sql.Contains("FROM acd.assignments a", StringComparison.OrdinalIgnoreCase))
                {
                    // Assignment 1: created 12 days ago, 10 scores recorded out of 20 enrolled -> Overdue!
                    // Assignment 2: created 2 days ago, 20 scores recorded out of 20 enrolled -> Not overdue.
                    var created12DaysAgo = targetDate.AddDays(-12).ToDateTime(TimeOnly.MinValue);
                    var created2DaysAgo = targetDate.AddDays(-2).ToDateTime(TimeOnly.MinValue);

                    return new FakeDbDataReader(
                        ["AssignmentId", "Title", "SubjectName", "TeacherName", "StudyGroupId", "StudyGroupName", "AssignedDate", "CreatedAt", "ActualScoresCount"],
                        [typeof(Guid), typeof(string), typeof(string), typeof(string), typeof(Guid), typeof(string), typeof(DateTime), typeof(DateTime), typeof(int)],
                        [
                            [Assignment1Id, "Tugas Aljabar", "Matematika", "Budi Santoso", StudyGroup1Id, "7A", created12DaysAgo, created12DaysAgo, 10],
                            [Assignment2Id, "Tugas Puisi", "Bahasa Indonesia", "Siti Rahma", StudyGroup2Id, "7B", created2DaysAgo, created2DaysAgo, 20]
                        ]);
                }

                if (sql.Contains("FROM acd.report_cards", StringComparison.OrdinalIgnoreCase))
                {
                    // StudyGroup1: 20 finalized, 0 draft
                    // StudyGroup2: 0 finalized, 20 draft -> Pending report cards alert!
                    return new FakeDbDataReader(
                        ["StudyGroupId", "DraftCount", "FinalizedCount", "TotalCount"],
                        [typeof(Guid), typeof(int), typeof(int), typeof(int)],
                        [
                            [StudyGroup1Id, 0, 20, 20],
                            [StudyGroup2Id, 20, 0, 20]
                        ]);
                }

                return new FakeDbDataReader([], [], []);
            },
            OnExecuteScalar = (sql, p) =>
            {
                if (sql.Contains("FROM opr.operational_periods", StringComparison.OrdinalIgnoreCase))
                {
                    return OperationalPeriodId;
                }
                return null;
            }
        };

        var handler = new GetAcademicMonitoringProgressHandler(new StubSqlConnectionFactory(() => connection));
        var query = new GetAcademicMonitoringProgressQuery(AcademicYearId, OperationalPeriodId, targetDate);

        var result = await handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        var dto = result.Value;
        Assert.NotNull(dto);

        // 1. Overview KPIs
        // Total enrolled = 20 + 20 = 40
        Assert.Equal(40, dto.Overview.TotalEnrolledStudentsCount);
        Assert.Equal(40, dto.Overview.TotalPlacedStudentsCount);

        // Attendance: 18 present + 2 late = 20 attended out of 40 enrolled = 50.00%
        Assert.Equal(18, dto.Overview.PresentCount);
        Assert.Equal(2, dto.Overview.LateCount);
        Assert.Equal(50.00m, dto.Overview.AttendanceRateTodayPercent);

        // Grading: Total expected = 20 + 20 = 40 scores. Actual = 10 + 20 = 30 scores. 30/40 = 75.00%
        Assert.Equal(2, dto.Overview.TotalAssignmentsCount);
        Assert.Equal(40, dto.Overview.ExpectedScoresCount);
        Assert.Equal(30, dto.Overview.ActualScoresCount);
        Assert.Equal(75.00m, dto.Overview.GradingCompletenessPercent);

        // Report cards: 20 finalized out of 40 = 50.00%
        Assert.Equal(20, dto.Overview.DraftReportCardsCount);
        Assert.Equal(20, dto.Overview.FinalizedReportCardsCount);
        Assert.Equal(50.00m, dto.Overview.ReportCardsFinalizedPercent);

        // 2. Exception Alerts
        // Missing attendance: StudyGroup2 has 0 attendance records
        var missingAtt = Assert.Single(dto.Exceptions.MissingAttendanceStudyGroups);
        Assert.Equal(StudyGroup2Id, missingAtt.StudyGroupId);
        Assert.Equal("7B", missingAtt.StudyGroupName);

        // Overdue grading: Assignment 1 (created 12 days ago, missing 10 scores)
        var overdue = Assert.Single(dto.Exceptions.OverdueGradingAssignments);
        Assert.Equal(Assignment1Id, overdue.AssignmentId);
        Assert.Equal("Tugas Aljabar", overdue.Title);
        Assert.Equal(5, overdue.DaysOverdue); // 12 - 7 = 5
        Assert.Equal(10, overdue.MissingScoresCount); // 20 - 10 = 10

        // Pending report cards: StudyGroup2 has 20 draft cards within 10 days of period end
        var pendingRc = Assert.Single(dto.Exceptions.PendingReportCards);
        Assert.Equal(StudyGroup2Id, pendingRc.StudyGroupId);
        Assert.Equal(20, pendingRc.DraftCount);

        // 3. Study Group Drill-down rows
        Assert.Equal(2, dto.StudyGroups.Count);
        var sg1Row = dto.StudyGroups.Single(r => r.StudyGroupId == StudyGroup1Id);
        Assert.False(sg1Row.IsAttendanceMissingToday);
        Assert.Equal(100.00m, sg1Row.AttendanceTodayPercent); // 20/20
        Assert.True(sg1Row.HasOverdueGrading);
        Assert.False(sg1Row.HasPendingReportCards);
        Assert.Equal("20/20", sg1Row.ReportCardsFinalizedRatio);

        var sg2Row = dto.StudyGroups.Single(r => r.StudyGroupId == StudyGroup2Id);
        Assert.True(sg2Row.IsAttendanceMissingToday);
        Assert.Equal(0.00m, sg2Row.AttendanceTodayPercent);
        Assert.False(sg2Row.HasOverdueGrading);
        Assert.True(sg2Row.HasPendingReportCards);
        Assert.Equal("0/20", sg2Row.ReportCardsFinalizedRatio);
    }

    [Fact]
    public async Task GetStudentReportCardsHandler_WhenCallerIsParent_FiltersOutDraftReportCards()
    {
        var connection = new FakeDbConnection
        {
            OnExecuteScalar = (sql, p) =>
            {
                if (sql.Contains("auth.user_students", StringComparison.OrdinalIgnoreCase))
                {
                    // Child is linked!
                    return 1;
                }
                return null;
            },
            OnExecuteReader = (sql, p) =>
            {
                // Verify SQL enforces status = 'FINALIZED'
                Assert.Contains("status = 'FINALIZED'", sql, StringComparison.OrdinalIgnoreCase);

                return new FakeDbDataReader(
                    ["Id", "StudentId", "StudentName", "AcademicYearName", "OperationalPeriodName", "StudyGroupName", "Status", "FinalizedAt"],
                    [typeof(Guid), typeof(Guid), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(DateTime)],
                    [
                        [FinalizedCardId, LinkedStudentId, "Ahmad Dahlan", "2026/2027", "Semester 1", "7A", "FINALIZED", DateTime.UtcNow]
                    ]);
            }
        };

        var parentUser = new TestCurrentUser(ParentUserId, ["PARENT"]);
        var handler = new GetStudentReportCardsHandler(new StubSqlConnectionFactory(() => connection), parentUser);

        var result = await handler.HandleAsync(new GetStudentReportCardsQuery(LinkedStudentId));

        Assert.True(result.IsSuccess);
        var cards = result.Value;
        Assert.NotNull(cards);
        var card = Assert.Single(cards);
        Assert.Equal("FINALIZED", card.Status);
        Assert.Equal(FinalizedCardId, card.Id);
    }

    [Fact]
    public async Task GetStudentReportCardsHandler_WhenParentAccessesUnlinkedStudent_ReturnsForbidden()
    {
        var connection = new FakeDbConnection
        {
            OnExecuteScalar = (sql, p) =>
            {
                // Child is NOT linked
                return null;
            }
        };

        var parentUser = new TestCurrentUser(ParentUserId, ["PARENT"]);
        var handler = new GetStudentReportCardsHandler(new StubSqlConnectionFactory(() => connection), parentUser);

        var result = await handler.HandleAsync(new GetStudentReportCardsQuery(OtherStudentId));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
    }

    [Fact]
    public async Task GetStudentReportCardsHandler_WhenCallerIsStaff_ReturnsDraftAndFinalized()
    {
        var connection = new FakeDbConnection
        {
            OnExecuteReader = (sql, p) =>
            {
                // Staff query does NOT enforce status = 'FINALIZED'
                Assert.DoesNotContain("status = 'FINALIZED'", sql, StringComparison.OrdinalIgnoreCase);

                return new FakeDbDataReader(
                    ["Id", "StudentId", "StudentName", "AcademicYearName", "OperationalPeriodName", "StudyGroupName", "Status", "FinalizedAt"],
                    [typeof(Guid), typeof(Guid), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(DateTime)],
                    [
                        [DraftCardId, LinkedStudentId, "Ahmad Dahlan", "2026/2027", "Semester 1", "7A", "DRAFT", null],
                        [FinalizedCardId, LinkedStudentId, "Ahmad Dahlan", "2026/2027", "Semester 1", "7A", "FINALIZED", DateTime.UtcNow]
                    ]);
            }
        };

        var staffUser = new TestCurrentUser(StaffUserId, ["TEACHER"]);
        var handler = new GetStudentReportCardsHandler(new StubSqlConnectionFactory(() => connection), staffUser);

        var result = await handler.HandleAsync(new GetStudentReportCardsQuery(LinkedStudentId));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task GetReportCardByIdHandler_WhenParentRequestsDraftCard_ReturnsNotFound404()
    {
        var connection = new FakeDbConnection
        {
            OnExecuteReader = (sql, p) =>
            {
                if (sql.Contains("FROM acd.report_cards", StringComparison.OrdinalIgnoreCase))
                {
                    return new FakeDbDataReader(
                        ["Id", "StudentId", "StudentName", "StudentNis", "StudentNisn", "AcademicYearId", "AcademicYearName", "OperationalPeriodId", "OperationalPeriodName", "StudyGroupId", "StudyGroupName", "HomeroomTeacherId", "HomeroomTeacherName", "Status", "OverrideReason", "GeneralRemarks", "FinalizedAt", "CreatedAt", "UpdatedAt"],
                        [typeof(Guid), typeof(Guid), typeof(string), typeof(string), typeof(string), typeof(Guid), typeof(string), typeof(Guid), typeof(string), typeof(Guid), typeof(string), typeof(Guid), typeof(string), typeof(string), typeof(string), typeof(string), typeof(DateTime), typeof(DateTime), typeof(DateTime)],
                        [
                            [DraftCardId, LinkedStudentId, "Ahmad Dahlan", "STD-001", null, AcademicYearId, "2026/2027", OperationalPeriodId, "Semester 1", StudyGroup1Id, "7A", Guid.NewGuid(), "Budi Santoso", "DRAFT", null, null, null, DateTime.UtcNow, DateTime.UtcNow]
                        ]);
                }

                return new FakeDbDataReader([], [], []);
            },
            OnExecuteScalar = (sql, p) =>
            {
                if (sql.Contains("auth.user_students", StringComparison.OrdinalIgnoreCase))
                {
                    return 1; // Linked
                }
                return null;
            }
        };

        var parentUser = new TestCurrentUser(ParentUserId, ["PARENT"]);
        var handler = new GetReportCardByIdHandler(new StubSqlConnectionFactory(() => connection), parentUser);

        var result = await handler.HandleAsync(DraftCardId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Acd.ReportCards.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task GetReportCardByIdHandler_WhenParentRequestsUnlinkedReportCard_ReturnsForbidden403()
    {
        var connection = new FakeDbConnection
        {
            OnExecuteReader = (sql, p) =>
            {
                if (sql.Contains("FROM acd.report_cards", StringComparison.OrdinalIgnoreCase))
                {
                    return new FakeDbDataReader(
                        ["Id", "StudentId", "StudentName", "StudentNis", "StudentNisn", "AcademicYearId", "AcademicYearName", "OperationalPeriodId", "OperationalPeriodName", "StudyGroupId", "StudyGroupName", "HomeroomTeacherId", "HomeroomTeacherName", "Status", "OverrideReason", "GeneralRemarks", "FinalizedAt", "CreatedAt", "UpdatedAt"],
                        [typeof(Guid), typeof(Guid), typeof(string), typeof(string), typeof(string), typeof(Guid), typeof(string), typeof(Guid), typeof(string), typeof(Guid), typeof(string), typeof(Guid), typeof(string), typeof(string), typeof(string), typeof(string), typeof(DateTime), typeof(DateTime), typeof(DateTime)],
                        [
                            [FinalizedCardId, OtherStudentId, "Other Student", "STD-999", null, AcademicYearId, "2026/2027", OperationalPeriodId, "Semester 1", StudyGroup1Id, "7A", Guid.NewGuid(), "Budi Santoso", "FINALIZED", null, null, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow]
                        ]);
                }

                return new FakeDbDataReader([], [], []);
            },
            OnExecuteScalar = (sql, p) =>
            {
                // Not linked!
                return null;
            }
        };

        var parentUser = new TestCurrentUser(ParentUserId, ["PARENT"]);
        var handler = new GetReportCardByIdHandler(new StubSqlConnectionFactory(() => connection), parentUser);

        var result = await handler.HandleAsync(FinalizedCardId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
    }

    [Fact]
    public async Task GetReportCardByIdHandler_WhenParentRequestsFinalizedLinkedCard_ReturnsSuccess()
    {
        var connection = new FakeDbConnection
        {
            OnExecuteReader = (sql, p) =>
            {
                if (sql.Contains("FROM acd.report_cards", StringComparison.OrdinalIgnoreCase))
                {
                    return new FakeDbDataReader(
                        ["Id", "StudentId", "StudentName", "StudentNis", "StudentNisn", "AcademicYearId", "AcademicYearName", "OperationalPeriodId", "OperationalPeriodName", "StudyGroupId", "StudyGroupName", "HomeroomTeacherId", "HomeroomTeacherName", "Status", "OverrideReason", "GeneralRemarks", "FinalizedAt", "CreatedAt", "UpdatedAt"],
                        [typeof(Guid), typeof(Guid), typeof(string), typeof(string), typeof(string), typeof(Guid), typeof(string), typeof(Guid), typeof(string), typeof(Guid), typeof(string), typeof(Guid), typeof(string), typeof(string), typeof(string), typeof(string), typeof(DateTime), typeof(DateTime), typeof(DateTime)],
                        [
                            [FinalizedCardId, LinkedStudentId, "Ahmad Dahlan", "STD-001", null, AcademicYearId, "2026/2027", OperationalPeriodId, "Semester 1", StudyGroup1Id, "7A", Guid.NewGuid(), "Budi Santoso", "FINALIZED", null, "Excellent progress", DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow]
                        ]);
                }

                if (sql.Contains("FROM acd.report_card_entries", StringComparison.OrdinalIgnoreCase))
                {
                    return new FakeDbDataReader(
                        ["Id", "SubjectId", "SubjectName", "SubjectCode", "SubjectStageId", "SubjectStageName", "TeacherId", "TeacherName", "NumericScore", "LetterGrade", "TeacherNotes"],
                        [typeof(Guid), typeof(Guid), typeof(string), typeof(string), typeof(Guid), typeof(string), typeof(Guid), typeof(string), typeof(decimal), typeof(string), typeof(string)],
                        [
                            [Guid.NewGuid(), Guid.NewGuid(), "Matematika", "MTK", Guid.NewGuid(), "MTK 7", Guid.NewGuid(), "Budi", 88.50m, "A", "Good job"]
                        ]);
                }

                if (sql.Contains("FROM acd.attendance_records", StringComparison.OrdinalIgnoreCase))
                {
                    return new FakeDbDataReader(
                        ["PresentCount", "AbsentCount", "SickCount", "LateCount", "ExcusedCount"],
                        [typeof(int), typeof(int), typeof(int), typeof(int), typeof(int)],
                        [
                            [25, 0, 1, 1, 0]
                        ]);
                }

                return new FakeDbDataReader([], [], []);
            },
            OnExecuteScalar = (sql, p) =>
            {
                if (sql.Contains("auth.user_students", StringComparison.OrdinalIgnoreCase))
                {
                    return 1; // Linked
                }
                return null;
            }
        };

        var parentUser = new TestCurrentUser(ParentUserId, ["PARENT"]);
        var handler = new GetReportCardByIdHandler(new StubSqlConnectionFactory(() => connection), parentUser);

        var result = await handler.HandleAsync(FinalizedCardId);

        Assert.True(result.IsSuccess);
        var detail = result.Value;
        Assert.NotNull(detail);
        Assert.Equal("FINALIZED", detail.Status);
        Assert.Equal("Ahmad Dahlan", detail.StudentName);
        Assert.Equal(88.50m, detail.AverageScore);
        Assert.Equal(25, detail.Attendance.PresentCount);
    }

    [Fact]
    public void DependencyInjection_RegistersAcdReadHandlers()
    {
        var services = new ServiceCollection();
        services.AddScoped<ISqlConnectionFactory, MockSqlConnectionFactory>();
        services.AddScoped<ICurrentUser>(_ => new TestCurrentUser(ParentUserId, ["ADMIN"]));
        services.AddAcdModule();

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<GetAcademicMonitoringProgressHandler>());
        Assert.NotNull(provider.GetService<GetStudentReportCardsHandler>());
        Assert.NotNull(provider.GetService<GetReportCardByIdHandler>());
    }

    private sealed class TestCurrentUser(Guid userId, IReadOnlyList<string>? roles = null) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid UserId => userId;
        public string Username => "test.user";
        public string FullName => "Test User";
        public IReadOnlyList<string> Roles => roles ?? ["PARENT"];
        public IReadOnlyList<string> Permissions => [];
        public Guid? TeacherId => null;
        public Guid? StudentId => null;
        public Guid? GuardianId => null;
    }

    private sealed class StubSqlConnectionFactory(Func<IDbConnection> factory) : ISqlConnectionFactory
    {
        public IDbConnection CreateConnection() => factory();
        public Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
        {
            var conn = factory();
            conn.Open();
            return Task.FromResult(conn);
        }
    }

    private sealed class MockSqlConnectionFactory : ISqlConnectionFactory
    {
        public IDbConnection CreateConnection() => throw new NotImplementedException();
        public Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class FakeDbConnection : DbConnection
    {
        private ConnectionState _state = ConnectionState.Closed;

        public Func<string, IDictionary<string, object?>, DbDataReader>? OnExecuteReader { get; init; }
        public Func<string, IDictionary<string, object?>, object?>? OnExecuteScalar { get; init; }

        [AllowNull]
        public override string ConnectionString { get; set; } = string.Empty;
        public override string Database => "Fake";
        public override string DataSource => "Fake";
        public override string ServerVersion => "1.0";
        public override ConnectionState State => _state;

        public override void Open() => _state = ConnectionState.Open;
        public override void Close() => _state = ConnectionState.Closed;
        public override void ChangeDatabase(string databaseName) { }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
            throw new NotSupportedException();

        protected override DbCommand CreateDbCommand() => new FakeDbCommand(this);

        private sealed class FakeDbCommand(FakeDbConnection connection) : DbCommand
        {
            private readonly FakeParameterCollection _parameters = new();

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
            public override void Prepare() { }
            protected override DbParameter CreateDbParameter() => new FakeDbParameter();

            public override int ExecuteNonQuery() => 0;

            public override object? ExecuteScalar()
            {
                var dict = _parameters.Cast<DbParameter>()
                    .ToDictionary(p => p.ParameterName.TrimStart('@'), p => p.Value == DBNull.Value ? null : p.Value);

                return connection.OnExecuteScalar?.Invoke(CommandText, dict);
            }

            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
            {
                var dict = _parameters.Cast<DbParameter>()
                    .ToDictionary(p => p.ParameterName.TrimStart('@'), p => p.Value == DBNull.Value ? null : p.Value);

                return connection.OnExecuteReader?.Invoke(CommandText, dict)
                       ?? throw new NotSupportedException();
            }
        }

        private sealed class FakeDbParameter : DbParameter
        {
            public override DbType DbType { get; set; }
            public override ParameterDirection Direction { get; set; }
            public override bool IsNullable { get; set; }
            [AllowNull]
            public override string ParameterName { get; set; } = string.Empty;
            public override int Size { get; set; }
            [AllowNull]
            public override string SourceColumn { get; set; } = string.Empty;
            public override bool SourceColumnNullMapping { get; set; }
            public override object? Value { get; set; }
            public override void ResetDbType() { }
        }

        private sealed class FakeParameterCollection : DbParameterCollection
        {
            private readonly List<object> _items = [];
            public override int Count => _items.Count;
            public override object SyncRoot => _items;
            public override int Add(object value) { _items.Add(value); return _items.Count - 1; }
            public override void AddRange(Array values) { foreach (var v in values) _items.Add(v!); }
            public override void Clear() => _items.Clear();
            public override bool Contains(object value) => _items.Contains(value);
            public override bool Contains(string value) => false;
            public override void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
            public override IEnumerator GetEnumerator() => _items.GetEnumerator();
            public override int IndexOf(object value) => _items.IndexOf(value);
            public override int IndexOf(string parameterName) => -1;
            public override void Insert(int index, object value) => _items.Insert(index, value);
            public override void Remove(object value) => _items.Remove(value);
            public override void RemoveAt(int index) => _items.RemoveAt(index);
            public override void RemoveAt(string parameterName) { }
            protected override DbParameter GetParameter(int index) => (DbParameter)_items[index];
            protected override DbParameter GetParameter(string parameterName) => throw new IndexOutOfRangeException();
            protected override void SetParameter(int index, DbParameter value) => _items[index] = value;
            protected override void SetParameter(string parameterName, DbParameter value) { }
        }
    }

    private sealed class FakeDbDataReader(string[] columns, Type[] fieldTypes, object?[][] rows) : DbDataReader
    {
        private int _index = -1;

        public override int FieldCount => columns.Length;
        public override bool HasRows => rows.Length > 0;
        public override bool IsClosed => false;
        public override int RecordsAffected => 0;
        public override int Depth => 0;
        public override object this[int ordinal] => GetValue(ordinal);
        public override object this[string name] => GetValue(GetOrdinal(name));

        public override bool Read() => ++_index < rows.Length;
        public override bool NextResult() => false;
        public override string GetName(int ordinal) => columns[ordinal];
        public override int GetOrdinal(string name) => Array.FindIndex(columns, c => string.Equals(c, name, StringComparison.OrdinalIgnoreCase));
        public override object GetValue(int ordinal) => rows[_index][ordinal] ?? DBNull.Value;
        public override bool IsDBNull(int ordinal) => rows[_index][ordinal] is null or DBNull;
        public override Type GetFieldType(int ordinal) => fieldTypes[ordinal];
        public override string GetDataTypeName(int ordinal) => GetFieldType(ordinal).Name;
        public override IEnumerator GetEnumerator() => rows.GetEnumerator();

        public override bool GetBoolean(int ordinal) => (bool)GetValue(ordinal);
        public override byte GetByte(int ordinal) => (byte)GetValue(ordinal);
        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => 0;
        public override char GetChar(int ordinal) => (char)GetValue(ordinal);
        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => 0;
        public override DateTime GetDateTime(int ordinal) => (DateTime)GetValue(ordinal);
        public override decimal GetDecimal(int ordinal) => (decimal)GetValue(ordinal);
        public override double GetDouble(int ordinal) => (double)GetValue(ordinal);
        public override float GetFloat(int ordinal) => (float)GetValue(ordinal);
        public override Guid GetGuid(int ordinal) => (Guid)GetValue(ordinal);
        public override short GetInt16(int ordinal) => (short)GetValue(ordinal);
        public override int GetInt32(int ordinal) => (int)GetValue(ordinal);
        public override long GetInt64(int ordinal) => (long)GetValue(ordinal);
        public override string GetString(int ordinal) => (string)GetValue(ordinal);
        public override int GetValues(object[] values)
        {
            var count = Math.Min(values.Length, FieldCount);
            for (var i = 0; i < count; i++) values[i] = GetValue(i);
            return count;
        }
    }
}
