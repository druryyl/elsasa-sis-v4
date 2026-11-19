using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using ElsasaSis.Modules.Std.Domain;
using ElsasaSis.Modules.Std.Infrastructure.Contracts;
using ElsasaSis.Modules.Std.Infrastructure.Persistence;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.UnitTests.Std;

public sealed class StdPersistenceAndContractTests
{
    [Fact]
    public async Task CandidateRepository_AddAndGetById_SavesInTransactionAndRehydrates()
    {
        var candidate = CandidateTests.CreateRegisteredCandidate();
        var recordedCommands = new List<RecordedCommand>();
        var db = new RecordingDbState(recordedCommands);

        db.EnqueueReaderResult(
            sql => sql.Contains("FROM std.candidates", StringComparison.Ordinal),
            CreateTable(
                [
                    ("id", typeof(Guid), candidate.Id),
                    ("admission_year_id", typeof(Guid), candidate.AdmissionYearId),
                    ("admission_year_name", typeof(string), candidate.AdmissionYearName),
                    ("full_name", typeof(string), candidate.FullName),
                    ("nickname", typeof(object), DBNull.Value),
                    ("date_of_birth", typeof(DateTime), candidate.DateOfBirth.ToDateTime(TimeOnly.MinValue)),
                    ("gender", typeof(string), candidate.Gender.ToCode()),
                    ("phone", typeof(object), DBNull.Value),
                    ("email", typeof(object), DBNull.Value),
                    ("address", typeof(object), DBNull.Value),
                    ("target_school_level", typeof(string), candidate.TargetSchoolLevel.ToCode()),
                    ("target_grade", typeof(string), candidate.TargetGrade),
                    ("admission_status", typeof(string), candidate.AdmissionStatus.ToCode()),
                    ("registered_at", typeof(DateTime), candidate.RegisteredAt),
                    ("decided_at", typeof(object), DBNull.Value),
                    ("decision_notes", typeof(object), DBNull.Value),
                    ("enrolled_student_id", typeof(object), DBNull.Value),
                    ("created_at", typeof(DateTime), candidate.CreatedAt),
                    ("updated_at", typeof(DateTime), candidate.UpdatedAt)
                ]));

        var guardian = candidate.Guardians[0];
        db.EnqueueReaderResult(
            sql => sql.Contains("FROM std.candidate_guardians", StringComparison.Ordinal),
            CreateTable(
                [
                    ("id", typeof(Guid), guardian.Id),
                    ("candidate_id", typeof(Guid), guardian.CandidateId),
                    ("full_name", typeof(string), guardian.FullName),
                    ("relationship_type", typeof(string), guardian.RelationshipType.ToCode()),
                    ("phone", typeof(string), guardian.Phone!),
                    ("email", typeof(object), DBNull.Value),
                    ("address", typeof(object), DBNull.Value),
                    ("created_at", typeof(DateTime), guardian.CreatedAt),
                    ("updated_at", typeof(DateTime), guardian.UpdatedAt)
                ]));

        var repo = new CandidateRepository(new FakeSqlConnectionFactory(db));
        await repo.AddAsync(candidate);

        Assert.Equal(1, db.CommittedTransactions);
        Assert.Contains(recordedCommands, c => c.Sql.Contains("INSERT INTO std.candidates", StringComparison.Ordinal));
        Assert.Contains(recordedCommands, c => c.Sql.Contains("INSERT INTO std.candidate_guardians", StringComparison.Ordinal));

        var loaded = await repo.GetByIdAsync(candidate.Id);
        Assert.NotNull(loaded);
        Assert.Equal(candidate.Id, loaded!.Id);
        Assert.Null(loaded.Nickname);
        Assert.Single(loaded.Guardians);
        Assert.Equal(guardian.FullName, loaded.Guardians[0].FullName);
        Assert.Null(loaded.Guardians[0].Email);
    }

    [Theory]
    [InlineData("Violation of UNIQUE KEY constraint 'UQ_students_nis'. Cannot insert duplicate key in object 'std.students'.", 2627, "Std.Student.DuplicateNis")]
    [InlineData("Cannot insert duplicate key row in object 'std.students' with unique index 'UQ_students_nisn'.", 2601, "Std.Student.DuplicateNisn")]
    public async Task StudentRepository_UpdateAsync_WhenUniqueViolationOccurs_MapsToConflictAndRollsBack(
        string sqlErrorMessage,
        int errorNumber,
        string expectedErrorCode)
    {
        var recordedCommands = new List<RecordedCommand>();
        var db = new RecordingDbState(recordedCommands)
        {
            ScalarHandler = _ => 0,
            NonQueryException = sql => sql.Contains("UPDATE std.students", StringComparison.Ordinal)
                ? new SqlException(errorNumber, sqlErrorMessage)
                : null
        };

        var candidate = CandidateTests.CreateRegisteredCandidate();
        Assert.True(candidate.Accept().IsSuccess);
        var student = Student.EnrollFromCandidate(candidate, "20260001", "0012345678").Value!;

        var repo = new StudentRepository(new FakeSqlConnectionFactory(db));
        var result = await repo.UpdateAsync(student);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal(expectedErrorCode, result.Error.Code);
        Assert.Equal(0, db.CommittedTransactions);
        Assert.Equal(1, db.RolledBackTransactions);
    }

    [Fact]
    public async Task StudentEnrollmentRepository_EnrollCandidateAsync_WhenNisOmitted_ComputesSequentialNisAndEnrollsAtomically()
    {
        var recordedCommands = new List<RecordedCommand>();
        var db = new RecordingDbState(recordedCommands)
        {
            ScalarHandler = sql =>
            {
                if (sql.Contains("LEN(nis) = 8", StringComparison.Ordinal))
                {
                    return 5;
                }

                return 0;
            },
            NonQueryRowsAffected = _ => 1
        };

        var candidate = CandidateTests.CreateRegisteredCandidate();
        Assert.True(candidate.Accept().IsSuccess);

        var repo = new StudentEnrollmentRepository(new FakeSqlConnectionFactory(db));
        var suggested = await repo.GetNextNisAsync(2026);
        Assert.Equal("20260005", suggested);

        var enrollDate = new DateOnly(2026, 7, 15);
        var result = await repo.EnrollCandidateAsync(candidate, requestedNis: null, nisn: "   ", enrollDate);

        Assert.True(result.IsSuccess);
        var student = result.Value!;
        Assert.Equal("20260005", student.Nis);
        Assert.Null(student.Nisn);
        Assert.Equal(AdmissionStatus.Enrolled, candidate.AdmissionStatus);
        Assert.Equal(1, db.CommittedTransactions);

        var seqCommand = recordedCommands.Last(c => c.Sql.Contains("LEN(nis) = 8", StringComparison.Ordinal));
        Assert.Contains("WITH (UPDLOCK, HOLDLOCK)", seqCommand.Sql, StringComparison.Ordinal);
        Assert.Contains("nis LIKE @YearPrefix + '[0-9][0-9][0-9][0-9]'", seqCommand.Sql, StringComparison.Ordinal);
        Assert.Contains(recordedCommands, c => c.Sql.Contains("INSERT INTO std.students", StringComparison.Ordinal));
        Assert.Contains(recordedCommands, c => c.Sql.Contains("INSERT INTO std.student_guardians", StringComparison.Ordinal));
        Assert.Contains(recordedCommands, c => c.Sql.Contains("UPDATE std.candidates", StringComparison.Ordinal));
    }

    [Fact]
    public async Task StudyGroupRepository_UpdateAsync_PropagatesStudyGroupNameToPlacementsPerExtStu002()
    {
        var recordedCommands = new List<RecordedCommand>();
        var db = new RecordingDbState(recordedCommands)
        {
            ScalarHandler = _ => 0,
            NonQueryRowsAffected = _ => 1
        };

        var studyGroup = StudyGroup.Create(
            Guid.NewGuid(),
            "2026/2027",
            SchoolLevel.SMP,
            "7",
            "7A").Value!;
        Assert.True(studyGroup.Update(SchoolLevel.SMP, "7", "7A-Unggulan").IsSuccess);

        var repo = new StudyGroupRepository(new FakeSqlConnectionFactory(db));
        var result = await repo.UpdateAsync(studyGroup);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, db.CommittedTransactions);
        Assert.Contains(recordedCommands, c => c.Sql.Contains("UPDATE std.study_groups", StringComparison.Ordinal));
        Assert.Contains(recordedCommands, c =>
            c.Sql.Contains("UPDATE std.student_placements", StringComparison.Ordinal) &&
            c.Sql.Contains("study_group_name = @Name", StringComparison.Ordinal));
    }

    [Fact]
    public async Task StudentPlacementRepository_UpsertAndGet_ExecutesUpsertAndRehydrates()
    {
        var recordedCommands = new List<RecordedCommand>();
        var db = new RecordingDbState(recordedCommands)
        {
            NonQueryRowsAffected = _ => 1
        };

        var placement = StudentPlacement.CreateOrUpdate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "2026/2027",
            SchoolLevel.SMP,
            "7",
            null,
            null).Value!;

        db.EnqueueReaderResult(
            sql => sql.Contains("FROM std.student_placements", StringComparison.Ordinal),
            CreateTable(
                [
                    ("id", typeof(Guid), placement.Id),
                    ("student_id", typeof(Guid), placement.StudentId),
                    ("academic_year_id", typeof(Guid), placement.AcademicYearId),
                    ("academic_year_name", typeof(string), placement.AcademicYearName),
                    ("school_level", typeof(string), placement.SchoolLevel.ToCode()),
                    ("grade", typeof(string), placement.Grade),
                    ("study_group_id", typeof(object), DBNull.Value),
                    ("study_group_name", typeof(object), DBNull.Value),
                    ("created_at", typeof(DateTime), placement.CreatedAt),
                    ("updated_at", typeof(DateTime), placement.UpdatedAt)
                ]));

        var repo = new StudentPlacementRepository(new FakeSqlConnectionFactory(db));
        var upsertResult = await repo.UpsertAsync(placement);

        Assert.True(upsertResult.IsSuccess);
        Assert.Equal(1, db.CommittedTransactions);
        Assert.Contains(recordedCommands, c =>
            c.Sql.Contains("UPDATE std.student_placements", StringComparison.Ordinal) &&
            c.Sql.Contains("IF @@ROWCOUNT = 0", StringComparison.Ordinal));

        var loaded = await repo.GetByStudentAndAcademicYearAsync(placement.StudentId, placement.AcademicYearId);
        Assert.NotNull(loaded);
        Assert.Equal(placement.Id, loaded!.Id);
        Assert.Null(loaded.StudyGroupId);
        Assert.Null(loaded.StudyGroupName);
    }

    [Fact]
    public async Task StudentContract_GetStudyGroupByIdAsync_ReturnsSummaryDto()
    {
        var groupId = Guid.NewGuid();
        var yearId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var recordedCommands = new List<RecordedCommand>();
        var db = new RecordingDbState(recordedCommands);

        db.EnqueueReaderResult(
            sql => sql.Contains("FROM std.study_groups", StringComparison.Ordinal),
            CreateTable(
                [
                    ("id", typeof(Guid), groupId),
                    ("academic_year_id", typeof(Guid), yearId),
                    ("academic_year_name", typeof(string), "2026/2027"),
                    ("school_level", typeof(string), "SMP"),
                    ("grade", typeof(string), "7"),
                    ("name", typeof(string), "7A"),
                    ("homeroom_teacher_id", typeof(Guid), teacherId),
                    ("homeroom_teacher_name", typeof(string), "Budi Santoso")
                ]));

        var contract = new StudentContract(new FakeSqlConnectionFactory(db));
        var dto = await contract.GetStudyGroupByIdAsync(groupId);

        Assert.NotNull(dto);
        Assert.Equal(groupId, dto!.Id);
        Assert.Equal(yearId, dto.AcademicYearId);
        Assert.Equal("2026/2027", dto.AcademicYearName);
        Assert.Equal("SMP", dto.SchoolLevel);
        Assert.Equal("7", dto.Grade);
        Assert.Equal("7A", dto.Name);
        Assert.Equal(teacherId, dto.HomeroomTeacherId);
        Assert.Equal("Budi Santoso", dto.HomeroomTeacherName);
    }

    private static DataTable CreateTable((string Name, Type Type, object Value)[] columns)
    {
        var table = new DataTable();
        foreach (var (name, type, _) in columns)
        {
            table.Columns.Add(name, type);
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
        public Func<string, object?>? ScalarHandler { get; init; }
        public Func<string, int>? NonQueryRowsAffected { get; init; }
        public Func<string, Exception?>? NonQueryException { get; init; }
        public int CommittedTransactions { get; set; }
        public int RolledBackTransactions { get; set; }

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

        public override void ChangeDatabase(string databaseName)
        {
        }

        public override void Close() => _connectionState = ConnectionState.Closed;

        public override void Open() => _connectionState = ConnectionState.Open;

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
            new FakeDbTransaction(this, state);

        protected override DbCommand CreateDbCommand() => new FakeDbCommand(this, state);
    }

    private sealed class FakeDbTransaction(DbConnection connection, RecordingDbState state) : DbTransaction
    {
        public override IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
        protected override DbConnection DbConnection => connection;

        public override void Commit() => state.CommittedTransactions++;

        public override void Rollback() => state.RolledBackTransactions++;
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

        public override void Cancel()
        {
        }

        public override int ExecuteNonQuery()
        {
            Record();
            var exception = state.NonQueryException?.Invoke(CommandText);
            if (exception is not null)
            {
                throw exception;
            }

            return state.NonQueryRowsAffected?.Invoke(CommandText) ?? 1;
        }

        public override object? ExecuteScalar()
        {
            Record();
            return state.ScalarHandler?.Invoke(CommandText);
        }

        public override void Prepare()
        {
        }

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

        public override void ResetDbType()
        {
        }
    }

    private sealed class FakeDbParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _items = [];

        public override int Count => _items.Count;
        public override object SyncRoot => ((ICollection)_items).SyncRoot;

        public override int Add(object value)
        {
            _items.Add((DbParameter)value);
            return _items.Count - 1;
        }

        public override void AddRange(Array values)
        {
            foreach (var v in values)
            {
                Add(v!);
            }
        }

        public override void Clear() => _items.Clear();
        public override bool Contains(object value) => _items.Contains((DbParameter)value);
        public override bool Contains(string value) => IndexOf(value) >= 0;
        public override void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
        public override IEnumerator GetEnumerator() => _items.GetEnumerator();
        public override int IndexOf(object value) => _items.IndexOf((DbParameter)value);
        public override int IndexOf(string parameterName) => _items.FindIndex(p => p.ParameterName == parameterName);
        public override void Insert(int index, object value) => _items.Insert(index, (DbParameter)value);
        public override void Remove(object value) => _items.Remove((DbParameter)value);
        public override void RemoveAt(int index) => _items.RemoveAt(index);
        public override void RemoveAt(string parameterName) => _items.RemoveAt(IndexOf(parameterName));
        protected override DbParameter GetParameter(int index) => _items[index];
        protected override DbParameter GetParameter(string parameterName) => _items[IndexOf(parameterName)];
        protected override void SetParameter(int index, DbParameter value) => _items[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value) => _items[IndexOf(parameterName)] = value;
    }

    private sealed class SqlException(int number, string message) : DbException(message)
    {
        public int Number { get; } = number;
    }
}
