using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using ElsasaSis.Contracts.Operational;
using ElsasaSis.Modules.Std.Features.Students.GetMyChildren;
using ElsasaSis.SharedKernel.Persistence;
using ElsasaSis.SharedKernel.Security;

namespace ElsasaSis.UnitTests.Std;

public sealed class GetMyChildrenHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenParentHasLinkedChildren_ReturnsExpectedStudentDtosOrderedByName()
    {
        var parentUserId = Guid.NewGuid();
        var activeYearId = Guid.NewGuid();
        var child1Id = Guid.NewGuid();
        var child2Id = Guid.NewGuid();
        var sg1Id = Guid.NewGuid();
        var sg2Id = Guid.NewGuid();

        var child1Dob = new DateTime(2010, 5, 10);
        var child2Dob = new DateTime(2012, 8, 15);

        string[] columns =
        [
            "StudentId", "Nis", "Nisn", "FullName", "Nickname", "DateOfBirth", "Gender",
            "StudyGroupId", "StudyGroupName", "SchoolLevel", "Grade"
        ];
        Type[] types =
        [
            typeof(Guid), typeof(string), typeof(string), typeof(string), typeof(string), typeof(DateTime), typeof(string),
            typeof(Guid), typeof(string), typeof(string), typeof(string)
        ];
        object?[][] rows =
        [
            [child1Id, "STD-001", "0012345678", "Ahmad Dahlan", "Ahmad", child1Dob, "MALE", sg1Id, "7A", "SMP", "7"],
            [child2Id, "STD-002", "0012345679", "Siti Nurhaliza", "Siti", child2Dob, "FEMALE", sg2Id, "5B", "SD", "5"]
        ];

        var executedCommands = new List<(string Sql, IDictionary<string, object?> Parameters)>();
        var connection = new FakeDbConnection
        {
            OnExecuteReader = (sql, parameters) =>
            {
                executedCommands.Add((sql, parameters));
                return new FakeDbDataReader(columns, types, rows);
            }
        };

        var handler = new GetMyChildrenHandler(
            new TestCurrentUser(parentUserId),
            new StubSqlConnectionFactory(() => connection),
            new TestOperationalContract(activeYearId));

        var result = await handler.HandleAsync(new GetMyChildrenQuery());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Count);

        var first = result.Value[0];
        Assert.Equal(child1Id, first.StudentId);
        Assert.Equal("STD-001", first.Nis);
        Assert.Equal("0012345678", first.Nisn);
        Assert.Equal("Ahmad Dahlan", first.FullName);
        Assert.Equal("Ahmad", first.Nickname);
        Assert.Equal(child1Dob, first.DateOfBirth);
        Assert.Equal("MALE", first.Gender);
        Assert.Equal(sg1Id, first.StudyGroupId);
        Assert.Equal("7A", first.StudyGroupName);
        Assert.Equal("SMP", first.SchoolLevel);
        Assert.Equal("7", first.Grade);

        var second = result.Value[1];
        Assert.Equal(child2Id, second.StudentId);
        Assert.Equal("STD-002", second.Nis);
        Assert.Equal("0012345679", second.Nisn);
        Assert.Equal("Siti Nurhaliza", second.FullName);
        Assert.Equal("Siti", second.Nickname);
        Assert.Equal(child2Dob, second.DateOfBirth);
        Assert.Equal("FEMALE", second.Gender);
        Assert.Equal(sg2Id, second.StudyGroupId);
        Assert.Equal("5B", second.StudyGroupName);
        Assert.Equal("SD", second.SchoolLevel);
        Assert.Equal("5", second.Grade);

        var cmd = Assert.Single(executedCommands);
        Assert.Contains("auth.user_students", cmd.Sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ORDER BY s.full_name ASC", cmd.Sql, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(parentUserId, cmd.Parameters["CurrentUserId"]);
        Assert.Equal(activeYearId, cmd.Parameters["ActiveAcademicYearId"]);
    }

    [Fact]
    public async Task HandleAsync_WhenParentHasZeroLinkedChildren_ReturnsEmptyList()
    {
        var parentUserId = Guid.NewGuid();
        var activeYearId = Guid.NewGuid();

        string[] columns =
        [
            "StudentId", "Nis", "Nisn", "FullName", "Nickname", "DateOfBirth", "Gender",
            "StudyGroupId", "StudyGroupName", "SchoolLevel", "Grade"
        ];
        Type[] types =
        [
            typeof(Guid), typeof(string), typeof(string), typeof(string), typeof(string), typeof(DateTime), typeof(string),
            typeof(Guid), typeof(string), typeof(string), typeof(string)
        ];
        object?[][] rows = [];

        var connection = new FakeDbConnection
        {
            OnExecuteReader = (_, _) => new FakeDbDataReader(columns, types, rows)
        };

        var handler = new GetMyChildrenHandler(
            new TestCurrentUser(parentUserId),
            new StubSqlConnectionFactory(() => connection),
            new TestOperationalContract(activeYearId));

        var result = await handler.HandleAsync(new GetMyChildrenQuery());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_ViaMediatRInterface_ReturnsSuccess()
    {
        var parentUserId = Guid.NewGuid();
        string[] columns =
        [
            "StudentId", "Nis", "Nisn", "FullName", "Nickname", "DateOfBirth", "Gender",
            "StudyGroupId", "StudyGroupName", "SchoolLevel", "Grade"
        ];
        Type[] types =
        [
            typeof(Guid), typeof(string), typeof(string), typeof(string), typeof(string), typeof(DateTime), typeof(string),
            typeof(Guid), typeof(string), typeof(string), typeof(string)
        ];
        object?[][] rows = [];

        var connection = new FakeDbConnection
        {
            OnExecuteReader = (_, _) => new FakeDbDataReader(columns, types, rows)
        };

        var handler = new GetMyChildrenHandler(
            new TestCurrentUser(parentUserId),
            new StubSqlConnectionFactory(() => connection),
            new TestOperationalContract(null));

        var result = await handler.Handle(new GetMyChildrenQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid UserId => userId;
        public string Username => "parent.test";
        public string FullName => "Parent Test";
        public IReadOnlyList<string> Roles => ["PARENT"];
        public IReadOnlyList<string> Permissions => ["Permissions.Students.Children.View"];
        public Guid? TeacherId => null;
        public Guid? StudentId => null;
        public Guid? GuardianId => null;
    }

    private sealed class TestOperationalContract(Guid? academicYearId) : IOperationalContract
    {
        public Task<ActiveAcademicYearDto?> GetActiveAcademicYearAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(academicYearId.HasValue
                ? new ActiveAcademicYearDto(academicYearId.Value, "2026/2027", new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30))
                : null);
        }

        public Task<ActiveOperationalPeriodDto?> GetActiveOperationalPeriodAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(academicYearId.HasValue
                ? new ActiveOperationalPeriodDto(Guid.NewGuid(), academicYearId.Value, "Semester 1", new DateOnly(2026, 7, 1), new DateOnly(2026, 12, 31))
                : null);
        }

        public Task<AcademicYearSummaryDto?> GetAcademicYearByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<AcademicYearSummaryDto?>(null);

        public Task<OperationalPeriodSummaryDto?> GetOperationalPeriodByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<OperationalPeriodSummaryDto?>(null);
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

    private sealed class FakeDbConnection : DbConnection
    {
        private ConnectionState _state = ConnectionState.Closed;

        public Func<string, IDictionary<string, object?>, DbDataReader>? OnExecuteReader { get; init; }

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
            public override object? ExecuteScalar() => null;

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
