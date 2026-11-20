using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using ElsasaSis.Contracts.Academic.Events;
using ElsasaSis.Contracts.Student;
using ElsasaSis.Modules.Std.Features.BillingLookup;
using ElsasaSis.Modules.Std.Infrastructure.Contracts;
using ElsasaSis.Modules.Std.Subscribers;
using ElsasaSis.SharedKernel.Persistence;
using Microsoft.Extensions.Logging.Abstractions;

namespace ElsasaSis.UnitTests.Std;

public sealed class StudentExtensionsTests
{
    [Theory]
    [InlineData("081234567890", "https://wa.me/6281234567890")]
    [InlineData("+6281234567890", "https://wa.me/6281234567890")]
    [InlineData("6281234567890", "https://wa.me/6281234567890")]
    [InlineData("81234567890", "https://wa.me/6281234567890")]
    [InlineData("0812-3456-7890", "https://wa.me/6281234567890")]
    [InlineData("+62 (812) 345-6789", "https://wa.me/628123456789")]
    public void FormatWhatsAppUrl_ValidIndonesianNumbers_FormatsCorrectly(string rawPhone, string expectedUrl)
    {
        var result = GetStudentBillingContactsHandler.FormatWhatsAppUrl(rawPhone);
        Assert.Equal(expectedUrl, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-phone-here")]
    [InlineData("---")]
    public void FormatWhatsAppUrl_InvalidOrEmptyNumbers_ReturnsNull(string? rawPhone)
    {
        var result = GetStudentBillingContactsHandler.FormatWhatsAppUrl(rawPhone);
        Assert.Null(result);
    }

    [Fact]
    public async Task LookupStudentBillingContactsHandler_QueriesAndMapsExpectedDtos()
    {
        var studentId = Guid.NewGuid();
        var studyGroupId = Guid.NewGuid();
        var recordedCommands = new List<RecordedCommand>();
        var db = new RecordingDbState(recordedCommands);

        db.EnqueueReaderResult(
            sql => sql.Contains("std.students", StringComparison.Ordinal),
            CreateTable(
                [
                    ("StudentId", typeof(Guid), studentId),
                    ("StudentNis", typeof(string), "20260001"),
                    ("StudentName", typeof(string), "Ahmad Dahlan"),
                    ("SchoolLevel", typeof(string), "SMP"),
                    ("Grade", typeof(string), "7"),
                    ("StudyGroupId", typeof(Guid), studyGroupId),
                    ("StudyGroupName", typeof(string), "7A"),
                    ("PrimaryGuardianName", typeof(string), "Budi Dahlan"),
                    ("PhoneNumber", typeof(string), "081234567890"),
                    ("Relationship", typeof(string), "Father")
                ]));

        var handler = new GetStudentBillingContactsHandler(new FakeSqlConnectionFactory(db));
        var query = new LookupStudentBillingContactsQuery("Ahmad");
        var result = await handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        var dtos = result.Value;
        Assert.Single(dtos);

        var dto = dtos[0];
        Assert.Equal(studentId, dto.StudentId);
        Assert.Equal("20260001", dto.StudentNis);
        Assert.Equal("Ahmad Dahlan", dto.StudentName);
        Assert.Equal("SMP", dto.SchoolLevel);
        Assert.Equal("7", dto.Grade);
        Assert.Equal(studyGroupId, dto.StudyGroupId);
        Assert.Equal("7A", dto.StudyGroupName);
        Assert.Equal("Budi Dahlan", dto.PrimaryGuardianName);
        Assert.Equal("081234567890", dto.PhoneNumber);
        Assert.Equal("Father", dto.Relationship);
        Assert.Equal("https://wa.me/6281234567890", dto.WhatsAppUrl);

        Assert.Single(recordedCommands);
        Assert.Equal("%Ahmad%", recordedCommands[0].Parameters["SearchPattern"]);
    }

    [Fact]
    public async Task LookupStudentBillingContactsHandler_WhenNoSearchFilter_PassesNullSearchPattern()
    {
        var recordedCommands = new List<RecordedCommand>();
        var db = new RecordingDbState(recordedCommands);

        db.EnqueueReaderResult(
            sql => sql.Contains("std.students", StringComparison.Ordinal),
            new DataTable());

        var handler = new GetStudentBillingContactsHandler(new FakeSqlConnectionFactory(db));
        var result = await handler.HandleAsync(new LookupStudentBillingContactsQuery(null));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
        Assert.Single(recordedCommands);
        var searchParam = recordedCommands[0].Parameters["SearchPattern"];
        Assert.True(searchParam is null || searchParam == DBNull.Value);
    }

    [Fact]
    public async Task ReportCardFinalizedEventHandler_ExecutesPlacementCompletionUpdate()
    {
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var reportCardId = Guid.NewGuid();
        var recordedCommands = new List<RecordedCommand>();
        var db = new RecordingDbState(recordedCommands)
        {
            NonQueryRowsAffected = _ => 1
        };

        var repo = new ElsasaSis.Modules.Std.Infrastructure.Persistence.StudentPlacementRepository(new FakeSqlConnectionFactory(db));
        var handler = new ReportCardFinalizedEventHandler(
            repo,
            NullLogger<ReportCardFinalizedEventHandler>.Instance);

        var domainEvent = new ReportCardFinalizedDomainEvent(
            reportCardId,
            studentId,
            periodId,
            DateTime.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        Assert.Single(recordedCommands);
        var command = recordedCommands[0];
        Assert.Contains("UPDATE std.student_placements", command.Sql, StringComparison.Ordinal);
        Assert.Contains("is_academically_completed = 1", command.Sql, StringComparison.Ordinal);
        Assert.Equal(studentId, command.Parameters["StudentId"]);
        Assert.Equal(periodId, command.Parameters["OperationalPeriodId"]);
        Assert.NotNull(command.Parameters["UtcNow"]);
    }

    [Fact]
    public async Task StudentContract_GetActiveStudentsByScopeAsync_QueriesAndMapsScopeDto()
    {
        var studentId = Guid.NewGuid();
        var studyGroupId = Guid.NewGuid();
        var recordedCommands = new List<RecordedCommand>();
        var db = new RecordingDbState(recordedCommands);

        db.EnqueueReaderResult(
            sql => sql.Contains("FROM RankedPlacements", StringComparison.Ordinal) || sql.Contains("std.student_placements", StringComparison.Ordinal),
            CreateTable(
                [
                    ("StudentId", typeof(Guid), studentId),
                    ("StudentName", typeof(string), "Ahmad Dahlan"),
                    ("StudentNis", typeof(string), "20260001"),
                    ("StudyGroupId", typeof(Guid), studyGroupId),
                    ("StudyGroupName", typeof(string), "7A"),
                    ("SchoolLevel", typeof(string), "SMP"),
                    ("Grade", typeof(string), "7")
                ]));

        var contract = new StudentContract(new FakeSqlConnectionFactory(db));
        var students = await contract.GetActiveStudentsByScopeAsync(studyGroupId, "SMP", "7");

        Assert.Single(students);
        var student = students[0];
        Assert.Equal(studentId, student.StudentId);
        Assert.Equal("Ahmad Dahlan", student.StudentName);
        Assert.Equal("20260001", student.StudentNis);
        Assert.Equal(studyGroupId, student.StudyGroupId);
        Assert.Equal("7A", student.StudyGroupName);
        Assert.Equal("SMP", student.SchoolLevel);
        Assert.Equal("7", student.Grade);

        Assert.Single(recordedCommands);
        Assert.Equal(studyGroupId, recordedCommands[0].Parameters["StudyGroupId"]);
        Assert.Equal("SMP", recordedCommands[0].Parameters["SchoolLevel"]);
        Assert.Equal("7", recordedCommands[0].Parameters["Grade"]);
    }

    private static DataTable CreateTable((string Name, Type Type, object Value)[] columns)
    {
        var table = new DataTable();
        foreach (var col in columns)
        {
            table.Columns.Add(col.Name, col.Type);
        }

        var row = table.NewRow();
        for (var i = 0; i < columns.Length; i++)
        {
            row[i] = columns[i].Value;
        }

        table.Rows.Add(row);
        return table;
    }

    private sealed record RecordedCommand(string Sql, Dictionary<string, object?> Parameters);

    private sealed class RecordingDbState(List<RecordedCommand> commands)
    {
        private readonly List<(Func<string, bool> Predicate, DataTable Table)> _readers = [];

        public List<RecordedCommand> Commands { get; } = commands;
        public Func<string, int>? NonQueryRowsAffected { get; init; }

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
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => new FakeDbTransaction(this);
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

        public override int ExecuteNonQuery()
        {
            Record();
            return state.NonQueryRowsAffected?.Invoke(CommandText) ?? 1;
        }

        public override object? ExecuteScalar()
        {
            Record();
            return null;
        }

        public override void Prepare() { }
        protected override DbParameter CreateDbParameter() => new FakeDbParameter();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            Record();
            return state.ConsumeReader(CommandText).CreateDataReader();
        }

        private void Record()
        {
            var dict = _parameters.Cast<DbParameter>().ToDictionary(p => p.ParameterName, p => p.Value);
            state.Commands.Add(new RecordedCommand(CommandText, dict));
        }
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
        public override object SyncRoot => ((ICollection)_parameters).SyncRoot;
        public override int Add(object value)
        {
            _parameters.Add((DbParameter)value);
            return _parameters.Count - 1;
        }
        public override void AddRange(Array values)
        {
            foreach (var value in values)
            {
                Add(value);
            }
        }
        public override void Clear() => _parameters.Clear();
        public override bool Contains(object value) => _parameters.Contains((DbParameter)value);
        public override bool Contains(string value) => _parameters.Any(p => p.ParameterName == value);
        public override void CopyTo(Array array, int index) => ((ICollection)_parameters).CopyTo(array, index);
        public override IEnumerator GetEnumerator() => _parameters.GetEnumerator();
        protected override DbParameter GetParameter(int index) => _parameters[index];
        protected override DbParameter GetParameter(string parameterName) =>
            _parameters.First(p => p.ParameterName == parameterName);
        public override int IndexOf(object value) => _parameters.IndexOf((DbParameter)value);
        public override int IndexOf(string parameterName) =>
            _parameters.FindIndex(p => p.ParameterName == parameterName);
        public override void Insert(int index, object value) => _parameters.Insert(index, (DbParameter)value);
        public override bool IsFixedSize => false;
        public override bool IsReadOnly => false;
        public override bool IsSynchronized => false;
        public override void Remove(object value) => _parameters.Remove((DbParameter)value);
        public override void RemoveAt(int index) => _parameters.RemoveAt(index);
        public override void RemoveAt(string parameterName)
        {
            var idx = IndexOf(parameterName);
            if (idx >= 0) _parameters.RemoveAt(idx);
        }
        protected override void SetParameter(int index, DbParameter value) => _parameters[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value)
        {
            var idx = IndexOf(parameterName);
            if (idx >= 0) _parameters[idx] = value;
            else _parameters.Add(value);
        }
    }
}
