using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ElsasaSis.Contracts.Teaching;
using ElsasaSis.Modules.Tch;
using ElsasaSis.Modules.Tch.Domain;
using ElsasaSis.Modules.Tch.Infrastructure.Contracts;
using ElsasaSis.Modules.Tch.Infrastructure.Persistence;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace ElsasaSis.UnitTests.Tch;

public sealed class TeacherPersistenceAndDiTests
{
    [Fact]
    public void AddTchModule_RegistersRepositoriesAndTeachingContract()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISqlConnectionFactory>(new StubSqlConnectionFactory(() => new FakeDbConnection()));

        services.AddTchModule();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetService<ITeacherRepository>());
        Assert.NotNull(scope.ServiceProvider.GetService<ITeacherAssignmentRepository>());
        Assert.NotNull(scope.ServiceProvider.GetService<ITeachingContract>());
    }

    [Fact]
    public void TchDomainLayer_HasZeroForbiddenNamespaceDependencies()
    {
        var domainTypes = typeof(TchAssemblyMarker).Assembly
            .GetTypes()
            .Where(t => t.Namespace is not null && t.Namespace.Contains(".Domain", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(domainTypes);

        string[] forbiddenPrefixes =
        [
            "ElsasaSis.Modules.Tch.Infrastructure",
            "ElsasaSis.Modules.Tch.Features",
            "Dapper",
            "System.Data",
            "Microsoft.Data.SqlClient",
            "Microsoft.AspNetCore.Http"
        ];

        foreach (var type in domainTypes)
        {
            var referencedTypes = new List<Type>();
            referencedTypes.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Select(f => f.FieldType));
            referencedTypes.AddRange(type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Select(p => p.PropertyType));
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                referencedTypes.Add(method.ReturnType);
                referencedTypes.AddRange(method.GetParameters().Select(p => p.ParameterType));
            }

            foreach (var referenced in referencedTypes)
            {
                var fullName = referenced.FullName ?? referenced.Namespace ?? string.Empty;
                Assert.DoesNotContain(forbiddenPrefixes, prefix => fullName.StartsWith(prefix, StringComparison.Ordinal));
            }
        }
    }

    [Fact]
    public async Task TeacherRepository_GetByIdAsync_RehydratesTeacherAndCapabilities()
    {
        var teacherId = Guid.NewGuid();
        var capabilityId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var dob = new DateTime(1985, 1, 15);
        var now = new DateTime(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc);

        var connection = new FakeDbConnection
        {
            OnExecuteReader = sql =>
            {
                if (sql.Contains("FROM tch.teachers", StringComparison.Ordinal))
                {
                    return new FakeDbDataReader(
                        ["id", "full_name", "nickname", "nip", "nik", "date_of_birth", "gender", "phone", "email", "address", "status", "created_at", "updated_at"],
                        [typeof(Guid), typeof(string), typeof(string), typeof(string), typeof(string), typeof(DateTime), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(DateTime), typeof(DateTime)],
                        [[teacherId, "Budi Santoso", null, "198501012010011001", null, dob, "MALE", "081234567890", null, null, "ACTIVE", now, now]]);
                }

                return new FakeDbDataReader(
                    ["id", "teacher_id", "subject_id", "subject_name", "created_at", "updated_at"],
                    [typeof(Guid), typeof(Guid), typeof(Guid), typeof(string), typeof(DateTime), typeof(DateTime)],
                    [[capabilityId, teacherId, subjectId, "Matematika", now, now]]);
            }
        };

        var repository = new TeacherRepository(new StubSqlConnectionFactory(() => connection));

        var teacher = await repository.GetByIdAsync(teacherId);

        Assert.NotNull(teacher);
        Assert.Equal(teacherId, teacher.Id);
        Assert.Equal("Budi Santoso", teacher.FullName);
        Assert.Equal(new DateOnly(1985, 1, 15), teacher.DateOfBirth);
        Assert.Equal(Gender.Male, teacher.Gender);
        Assert.Equal(TeacherStatus.Active, teacher.Status);
        var cap = Assert.Single(teacher.Capabilities);
        Assert.Equal(capabilityId, cap.Id);
        Assert.Equal(subjectId, cap.SubjectId);
        Assert.Equal("Matematika", cap.SubjectName);
    }

    [Theory]
    [InlineData("Cannot insert duplicate key row in object 'tch.teachers' with unique index 'UQ_teachers_nip'.", "Tch.Teacher.DuplicateNip")]
    [InlineData("Cannot insert duplicate key row in object 'tch.teachers' with unique index 'UQ_teachers_nik'.", "Tch.Teacher.DuplicateNik")]
    public async Task TeacherRepository_AddAsync_WhenNipOrNikUniqueViolation_MapsToConflict(
        string sqlErrorMessage,
        string expectedErrorCode)
    {
        var teacher = Teacher.Register(
            fullName: "Budi Santoso",
            dateOfBirth: new DateOnly(1985, 1, 1),
            gender: Gender.Male,
            nip: "198501012010011001",
            nik: "3271010101850001",
            phone: "081234567890").Value!;

        var connection = new FakeDbConnection
        {
            OnExecuteNonQuery = sql =>
            {
                if (sql.Contains("INSERT INTO tch.teachers", StringComparison.Ordinal))
                {
                    throw new FakeSqlException(2601, sqlErrorMessage);
                }

                return 1;
            }
        };

        var repository = new TeacherRepository(new StubSqlConnectionFactory(() => connection));

        var result = await repository.AddAsync(teacher);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal(expectedErrorCode, result.Error.Code);
        Assert.True(connection.WasRolledBack);
    }

    [Fact]
    public async Task TeacherRepository_UpdateAsync_WhenFullNameChanged_RefreshesAssignmentTeacherName()
    {
        var teacher = Teacher.Register(
            fullName: "Budi Santoso",
            dateOfBirth: new DateOnly(1985, 1, 1),
            gender: Gender.Male,
            phone: "081234567890").Value!;

        teacher.Update(
            fullName: "Budi Santoso, M.Pd.",
            dateOfBirth: new DateOnly(1985, 1, 1),
            gender: Gender.Male,
            phone: "081234567890");

        var executedCommands = new List<string>();
        var connection = new FakeDbConnection
        {
            OnExecuteScalar = sql => sql.Contains("SELECT full_name", StringComparison.Ordinal)
                ? "Budi Santoso"
                : null,
            OnExecuteNonQuery = sql =>
            {
                executedCommands.Add(sql);
                return 1;
            }
        };

        var repository = new TeacherRepository(new StubSqlConnectionFactory(() => connection));

        var result = await repository.UpdateAsync(teacher);

        Assert.True(result.IsSuccess);
        Assert.True(connection.WasCommitted);
        Assert.Contains(executedCommands, sql =>
            sql.Contains("UPDATE tch.teacher_assignments", StringComparison.Ordinal) &&
            sql.Contains("teacher_name = @TeacherName", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TeacherAssignmentRepository_GetByTupleAsync_RehydratesRemovedAssignmentForReactivation()
    {
        var assignmentId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var yearId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var studyGroupId = Guid.NewGuid();
        var now = new DateTime(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc);

        var connection = new FakeDbConnection
        {
            OnExecuteReader = _ => new FakeDbDataReader(
                [
                    "id", "teacher_id", "teacher_name", "academic_year_id", "academic_year_name",
                    "operational_period_id", "operational_period_name", "subject_id", "subject_name",
                    "study_group_id", "study_group_name", "status", "created_at", "updated_at"
                ],
                [
                    typeof(Guid), typeof(Guid), typeof(string), typeof(Guid), typeof(string),
                    typeof(Guid), typeof(string), typeof(Guid), typeof(string),
                    typeof(Guid), typeof(string), typeof(string), typeof(DateTime), typeof(DateTime)
                ],
                [[
                    assignmentId, teacherId, "Budi Santoso", yearId, "2026/2027",
                    periodId, "Semester 1", subjectId, "Matematika",
                    studyGroupId, "7A", "REMOVED", now, now
                ]])
        };

        var repository = new TeacherAssignmentRepository(new StubSqlConnectionFactory(() => connection));

        var assignment = await repository.GetByTupleAsync(teacherId, subjectId, studyGroupId, periodId);

        Assert.NotNull(assignment);
        Assert.Equal(assignmentId, assignment.Id);
        Assert.Equal(AssignmentStatus.Removed, assignment.Status);
        Assert.Equal("7A", assignment.StudyGroupName);
    }

    [Fact]
    public async Task TeacherAssignmentRepository_AddAsync_WhenUniqueConstraintViolated_MapsToConflict()
    {
        var teacher = Teacher.Register(
            fullName: "Budi Santoso",
            dateOfBirth: new DateOnly(1985, 1, 1),
            gender: Gender.Male,
            phone: "081234567890").Value!;
        var subjectId = Guid.NewGuid();
        teacher.AddCapability(subjectId, "Matematika");

        var assignment = TeacherAssignment.Assign(
            teacher,
            Guid.NewGuid(),
            "2026/2027",
            Guid.NewGuid(),
            "Semester 1",
            subjectId,
            "Matematika",
            Guid.NewGuid(),
            "7A").Value!;

        var connection = new FakeDbConnection
        {
            OnExecuteNonQuery = _ => throw new FakeSqlException(
                2627,
                "Violation of UNIQUE KEY constraint 'UQ_teacher_assignments_teacher_subject_group_period'.")
        };

        var repository = new TeacherAssignmentRepository(new StubSqlConnectionFactory(() => connection));

        var result = await repository.AddAsync(assignment);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Tch.Assignment.Duplicate", result.Error.Code);
        Assert.True(connection.WasRolledBack);
    }

    [Fact]
    public async Task TeachingContract_GetTeacherByIdAsync_ReturnsSummaryDto()
    {
        var teacherId = Guid.NewGuid();
        var connection = new FakeDbConnection
        {
            OnExecuteReader = _ => new FakeDbDataReader(
                ["id", "full_name", "status"],
                [typeof(Guid), typeof(string), typeof(string)],
                [[teacherId, "Budi Santoso", "ACTIVE"]])
        };

        var contract = new TeachingContract(new StubSqlConnectionFactory(() => connection));

        var dto = await contract.GetTeacherByIdAsync(teacherId);

        Assert.NotNull(dto);
        Assert.Equal(teacherId, dto.Id);
        Assert.Equal("Budi Santoso", dto.FullName);
        Assert.Equal("ACTIVE", dto.Status);
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

    private sealed class FakeSqlException(int number, string message) : DbException(message)
    {
        public int Number { get; } = number;
    }

    private sealed class FakeDbConnection : DbConnection
    {
        private ConnectionState _state = ConnectionState.Closed;

        public Func<string, int>? OnExecuteNonQuery { get; init; }
        public Func<string, object?>? OnExecuteScalar { get; init; }
        public Func<string, DbDataReader>? OnExecuteReader { get; init; }
        public bool WasCommitted { get; private set; }
        public bool WasRolledBack { get; private set; }

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
            new FakeDbTransaction(this);

        protected override DbCommand CreateDbCommand() => new FakeDbCommand(this);

        private sealed class FakeDbTransaction(FakeDbConnection connection) : DbTransaction
        {
            public override IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
            protected override DbConnection DbConnection => connection;
            public override void Commit() => connection.WasCommitted = true;
            public override void Rollback() => connection.WasRolledBack = true;
        }

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

            public override int ExecuteNonQuery() =>
                connection.OnExecuteNonQuery?.Invoke(CommandText) ?? 1;

            public override object? ExecuteScalar() =>
                connection.OnExecuteScalar?.Invoke(CommandText);

            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
                connection.OnExecuteReader?.Invoke(CommandText)
                ?? throw new NotSupportedException();
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
        public override int GetValues(object[] values) { var count = Math.Min(values.Length, FieldCount); for (var i = 0; i < count; i++) values[i] = GetValue(i); return count; }
    }
}
