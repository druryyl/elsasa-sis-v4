using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using ElsasaSis.Contracts.Student;
using ElsasaSis.Modules.Auth;
using ElsasaSis.Modules.Fin.Features.Payments.GetPaymentReceiptById;
using ElsasaSis.Modules.Fin.Features.Payments.GetStudentStatement;
using ElsasaSis.Modules.Fin.Features.Reports;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;
using ElsasaSis.SharedKernel.Security;
using Microsoft.AspNetCore.Http;
using Xunit;
using AuthPermissions = ElsasaSis.Modules.Auth.Permissions;
using FinPermissions = ElsasaSis.Modules.Fin.Permissions;

namespace ElsasaSis.UnitTests.Fin;

public sealed class FinanceChildScopingTests
{
    private static readonly Guid ParentUserId = Guid.NewGuid();
    private static readonly Guid LinkedStudentId = Guid.NewGuid();
    private static readonly Guid UnlinkedStudentId = Guid.NewGuid();
    private static readonly Guid ReceiptId = Guid.NewGuid();
    private static readonly Guid AcademicYearId = Guid.NewGuid();

    [Fact]
    public async Task GetStudentStatementHandler_WhenParentQueriesUnlinkedStudent_ReturnsForbidden()
    {
        // Arrange
        var parentUser = new MockCurrentUser(
            ParentUserId,
            "parent.hendra",
            "Hendra Dahlan",
            [RolePermissionCatalog.Roles.Parent],
            [FinPermissions.Finance.Statements.View]);

        var connection = new FakeDbConnection
        {
            OnExecuteScalar = (sql, _) =>
            {
                if (sql.Contains("FROM auth.user_students", StringComparison.OrdinalIgnoreCase))
                {
                    // Unlinked: return null
                    return null;
                }

                return null;
            }
        };

        var factory = new StubSqlConnectionFactory(() => connection);
        var studentContract = new StubStudentContract();
        var handler = new GetStudentStatementHandler(factory, studentContract, parentUser);

        // Act
        var result = await handler.HandleAsync(new GetStudentStatementQuery(UnlinkedStudentId));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        Assert.Equal("Auth.Forbidden", result.Error.Code);
        Assert.Equal("You do not have permission to view financial statements for this student.", result.Error.Message);
    }

    [Fact]
    public async Task GetStudentStatementHandler_WhenParentQueriesLinkedStudent_Succeeds()
    {
        // Arrange
        var parentUser = new MockCurrentUser(
            ParentUserId,
            "parent.hendra",
            "Hendra Dahlan",
            [RolePermissionCatalog.Roles.Parent],
            [FinPermissions.Finance.Statements.View]);

        var connection = new FakeDbConnection
        {
            OnExecuteScalar = (sql, _) =>
            {
                if (sql.Contains("FROM auth.user_students", StringComparison.OrdinalIgnoreCase))
                {
                    // Linked: return 1
                    return 1;
                }

                return null;
            },
            OnExecuteReader = (sql, _) =>
            {
                if (sql.Contains("FROM fin.student_charges", StringComparison.OrdinalIgnoreCase))
                {
                    return FakeDbDataReader.SingleResult(
                        [
                            "Id", "AcademicYearId", "AcademicYearName", "OperationalPeriodId", "OperationalPeriodName",
                            "BillingMonth", "ChargeTypeId", "ChargeTypeName", "ChargeFrequency", "StudentId",
                            "StudentName", "StudentNis", "StudyGroupId", "StudyGroupName", "Description",
                            "UnitAmount", "Quantity", "GrossAmount", "DiscountAmount", "DiscountReason",
                            "NetAmount", "PaidAmount", "OutstandingAmount", "Status", "DueDate", "CreatedAt"
                        ],
                        [
                            typeof(Guid), typeof(Guid), typeof(string), typeof(Guid), typeof(string),
                            typeof(string), typeof(Guid), typeof(string), typeof(string), typeof(Guid),
                            typeof(string), typeof(string), typeof(Guid), typeof(string), typeof(string),
                            typeof(decimal), typeof(int), typeof(decimal), typeof(decimal), typeof(string),
                            typeof(decimal), typeof(decimal), typeof(decimal), typeof(string), typeof(DateTime), typeof(DateTime)
                        ],
                        [
                            [
                                Guid.NewGuid(), AcademicYearId, "2026/2027", Guid.NewGuid(), "Semester 1",
                                "2026-07", Guid.NewGuid(), "SPP Bulanan", "MONTHLY", LinkedStudentId,
                                "Ahmad Dahlan", "2026001", Guid.NewGuid(), "VII-A", "SPP Juli",
                                350_000m, 1, 350_000m, 0m, null,
                                350_000m, 350_000m, 0m, "PAID", DateTime.UtcNow.AddDays(10), DateTime.UtcNow
                            ]
                        ]);
                }

                if (sql.Contains("FROM fin.payment_receipts", StringComparison.OrdinalIgnoreCase))
                {
                    return FakeDbDataReader.SingleResult(
                        [
                            "Id", "ReceiptNumber", "AcademicYearId", "AcademicYearName", "StudentId",
                            "StudentName", "StudentNis", "StudyGroupId", "StudyGroupName", "PaymentDate",
                            "PaymentMethod", "BankReference", "TotalAmount", "AllocatedAmount", "UnallocatedAmount",
                            "ReceivedById", "ReceivedByName", "Notes", "CreatedAt"
                        ],
                        [
                            typeof(Guid), typeof(string), typeof(Guid), typeof(string), typeof(Guid),
                            typeof(string), typeof(string), typeof(Guid), typeof(string), typeof(DateTime),
                            typeof(string), typeof(string), typeof(decimal), typeof(decimal), typeof(decimal),
                            typeof(Guid), typeof(string), typeof(string), typeof(DateTime)
                        ],
                        [
                            [
                                ReceiptId, "KWT-202607-0001", AcademicYearId, "2026/2027", LinkedStudentId,
                                "Ahmad Dahlan", "2026001", Guid.NewGuid(), "VII-A", DateTime.UtcNow,
                                "CASH", null, 350_000m, 350_000m, 0m,
                                Guid.NewGuid(), "Kasir", null, DateTime.UtcNow
                            ]
                        ]);
                }

                if (sql.Contains("FROM fin.payment_allocations", StringComparison.OrdinalIgnoreCase))
                {
                    return FakeDbDataReader.SingleResult(
                        [
                            "Id", "PaymentReceiptId", "ReceiptNumber", "StudentChargeId", "ChargeTypeName",
                            "BillingMonth", "StudentId", "StudentName", "StudentNis", "AllocatedAmount",
                            "AllocationDate", "CreatedAt"
                        ],
                        [
                            typeof(Guid), typeof(Guid), typeof(string), typeof(Guid), typeof(string),
                            typeof(string), typeof(Guid), typeof(string), typeof(string), typeof(decimal),
                            typeof(DateTime), typeof(DateTime)
                        ],
                        []);
                }

                return FakeDbDataReader.Empty();
            }
        };

        var factory = new StubSqlConnectionFactory(() => connection);
        var studentContract = new StubStudentContract();
        var handler = new GetStudentStatementHandler(factory, studentContract, parentUser);

        // Act
        var result = await handler.HandleAsync(new GetStudentStatementQuery(LinkedStudentId));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(LinkedStudentId, result.Value!.StudentId);
        Assert.Equal("Ahmad Dahlan", result.Value.StudentName);
        Assert.Single(result.Value.Charges);
        Assert.Single(result.Value.Receipts);
    }

    [Fact]
    public async Task GetStudentStatementHandler_WhenStaffQueries_BypassesChildScoping()
    {
        // Arrange - Finance Officer caller
        var staffUser = new MockCurrentUser(
            Guid.NewGuid(),
            "finance.officer",
            "Finance Officer",
            [RolePermissionCatalog.Roles.FinanceOfficer],
            [Permissions.Finance.Statements.View]);

        var executedChildCheck = false;

        var connection = new FakeDbConnection
        {
            OnExecuteScalar = (sql, _) =>
            {
                if (sql.Contains("FROM auth.user_students", StringComparison.OrdinalIgnoreCase))
                {
                    executedChildCheck = true;
                    return null;
                }

                return null;
            },
            OnExecuteReader = (sql, _) =>
            {
                if (sql.Contains("FROM fin.student_charges", StringComparison.OrdinalIgnoreCase))
                {
                    return FakeDbDataReader.SingleResult(
                        [
                            "Id", "AcademicYearId", "AcademicYearName", "OperationalPeriodId", "OperationalPeriodName",
                            "BillingMonth", "ChargeTypeId", "ChargeTypeName", "ChargeFrequency", "StudentId",
                            "StudentName", "StudentNis", "StudyGroupId", "StudyGroupName", "Description",
                            "UnitAmount", "Quantity", "GrossAmount", "DiscountAmount", "DiscountReason",
                            "NetAmount", "PaidAmount", "OutstandingAmount", "Status", "DueDate", "CreatedAt"
                        ],
                        [
                            typeof(Guid), typeof(Guid), typeof(string), typeof(Guid), typeof(string),
                            typeof(string), typeof(Guid), typeof(string), typeof(string), typeof(Guid),
                            typeof(string), typeof(string), typeof(Guid), typeof(string), typeof(string),
                            typeof(decimal), typeof(int), typeof(decimal), typeof(decimal), typeof(string),
                            typeof(decimal), typeof(decimal), typeof(decimal), typeof(string), typeof(DateTime), typeof(DateTime)
                        ],
                        [
                            [
                                Guid.NewGuid(), AcademicYearId, "2026/2027", Guid.NewGuid(), "Semester 1",
                                "2026-07", Guid.NewGuid(), "SPP Bulanan", "MONTHLY", UnlinkedStudentId,
                                "Siti Nurhaliza", "2026002", Guid.NewGuid(), "VII-A", "SPP Juli",
                                350_000m, 1, 350_000m, 0m, null,
                                350_000m, 350_000m, 0m, "PAID", DateTime.UtcNow.AddDays(10), DateTime.UtcNow
                            ]
                        ]);
                }

                if (sql.Contains("FROM fin.payment_receipts", StringComparison.OrdinalIgnoreCase))
                {
                    return FakeDbDataReader.SingleResult(
                        [
                            "Id", "ReceiptNumber", "AcademicYearId", "AcademicYearName", "StudentId",
                            "StudentName", "StudentNis", "StudyGroupId", "StudyGroupName", "PaymentDate",
                            "PaymentMethod", "BankReference", "TotalAmount", "AllocatedAmount", "UnallocatedAmount",
                            "ReceivedById", "ReceivedByName", "Notes", "CreatedAt"
                        ],
                        [
                            typeof(Guid), typeof(string), typeof(Guid), typeof(string), typeof(Guid),
                            typeof(string), typeof(string), typeof(Guid), typeof(string), typeof(DateTime),
                            typeof(string), typeof(string), typeof(decimal), typeof(decimal), typeof(decimal),
                            typeof(Guid), typeof(string), typeof(string), typeof(DateTime)
                        ],
                        []);
                }

                if (sql.Contains("FROM fin.payment_allocations", StringComparison.OrdinalIgnoreCase))
                {
                    return FakeDbDataReader.SingleResult(
                        [
                            "Id", "PaymentReceiptId", "ReceiptNumber", "StudentChargeId", "ChargeTypeName",
                            "BillingMonth", "StudentId", "StudentName", "StudentNis", "AllocatedAmount",
                            "AllocationDate", "CreatedAt"
                        ],
                        [
                            typeof(Guid), typeof(Guid), typeof(string), typeof(Guid), typeof(string),
                            typeof(string), typeof(Guid), typeof(string), typeof(string), typeof(decimal),
                            typeof(DateTime), typeof(DateTime)
                        ],
                        []);
                }

                return FakeDbDataReader.Empty();
            }
        };

        var factory = new StubSqlConnectionFactory(() => connection);
        var studentContract = new StubStudentContract();
        var handler = new GetStudentStatementHandler(factory, studentContract, staffUser);

        // Act
        var result = await handler.HandleAsync(new GetStudentStatementQuery(UnlinkedStudentId));

        // Assert
        Assert.False(executedChildCheck);
        Assert.True(result.IsSuccess);
        Assert.Equal("Siti Nurhaliza", result.Value!.StudentName);
    }

    [Fact]
    public async Task GetPaymentReceiptByIdHandler_WhenParentQueriesReceiptForUnlinkedStudent_ReturnsForbidden()
    {
        // Arrange
        var parentUser = new MockCurrentUser(
            ParentUserId,
            "parent.hendra",
            "Hendra Dahlan",
            [RolePermissionCatalog.Roles.Parent],
            [FinPermissions.Finance.Statements.View]);

        var connection = new FakeDbConnection
        {
            OnExecuteReader = (sql, _) =>
            {
                if (sql.Contains("FROM fin.payment_receipts", StringComparison.OrdinalIgnoreCase))
                {
                    // Multi-result reader: result 1 = receipt, result 2 = allocations
                    return new FakeDbDataReader(
                        [
                            // Result Set 1: Receipt for Unlinked Student
                            (
                                [
                                    "Id", "ReceiptNumber", "AcademicYearId", "AcademicYearName", "StudentId",
                                    "StudentName", "StudentNis", "StudyGroupId", "StudyGroupName", "PaymentDate",
                                    "PaymentMethod", "BankReference", "TotalAmount", "AllocatedAmount", "UnallocatedAmount",
                                    "ReceivedById", "ReceivedByName", "Notes", "CreatedAt"
                                ],
                                [
                                    typeof(Guid), typeof(string), typeof(Guid), typeof(string), typeof(Guid),
                                    typeof(string), typeof(string), typeof(Guid), typeof(string), typeof(DateTime),
                                    typeof(string), typeof(string), typeof(decimal), typeof(decimal), typeof(decimal),
                                    typeof(Guid), typeof(string), typeof(string), typeof(DateTime)
                                ],
                                [
                                    [
                                        ReceiptId, "KWT-202607-0099", AcademicYearId, "2026/2027", UnlinkedStudentId,
                                        "Other Student", "2026099", Guid.NewGuid(), "VII-B", DateTime.UtcNow,
                                        "CASH", null, 350_000m, 350_000m, 0m,
                                        Guid.NewGuid(), "Kasir", null, DateTime.UtcNow
                                    ]
                                ]
                            ),
                            // Result Set 2: Allocations
                            (
                                [
                                    "Id", "StudentChargeId", "ChargeTypeName", "BillingMonth", "AllocatedAmount",
                                    "AllocationDate", "CreatedAt"
                                ],
                                [
                                    typeof(Guid), typeof(Guid), typeof(string), typeof(string), typeof(decimal),
                                    typeof(DateTime), typeof(DateTime)
                                ],
                                []
                            )
                        ]);
                }

                return FakeDbDataReader.Empty();
            },
            OnExecuteScalar = (sql, _) =>
            {
                if (sql.Contains("FROM auth.user_students", StringComparison.OrdinalIgnoreCase))
                {
                    // Unlinked: return null
                    return null;
                }

                return null;
            }
        };

        var factory = new StubSqlConnectionFactory(() => connection);
        var handler = new GetPaymentReceiptByIdHandler(factory, parentUser);

        // Act
        var result = await handler.HandleAsync(new GetPaymentReceiptByIdQuery(ReceiptId));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        Assert.Equal("Auth.Forbidden", result.Error.Code);
        Assert.Equal("You do not have permission to view this payment receipt.", result.Error.Message);
    }

    [Fact]
    public async Task GetPaymentReceiptByIdHandler_WhenParentQueriesReceiptForLinkedStudent_Succeeds()
    {
        // Arrange
        var parentUser = new MockCurrentUser(
            ParentUserId,
            "parent.hendra",
            "Hendra Dahlan",
            [RolePermissionCatalog.Roles.Parent],
            [FinPermissions.Finance.Statements.View]);

        var connection = new FakeDbConnection
        {
            OnExecuteReader = (sql, _) =>
            {
                if (sql.Contains("FROM fin.payment_receipts", StringComparison.OrdinalIgnoreCase))
                {
                    return new FakeDbDataReader(
                        [
                            (
                                [
                                    "Id", "ReceiptNumber", "AcademicYearId", "AcademicYearName", "StudentId",
                                    "StudentName", "StudentNis", "StudyGroupId", "StudyGroupName", "PaymentDate",
                                    "PaymentMethod", "BankReference", "TotalAmount", "AllocatedAmount", "UnallocatedAmount",
                                    "ReceivedById", "ReceivedByName", "Notes", "CreatedAt"
                                ],
                                [
                                    typeof(Guid), typeof(string), typeof(Guid), typeof(string), typeof(Guid),
                                    typeof(string), typeof(string), typeof(Guid), typeof(string), typeof(DateTime),
                                    typeof(string), typeof(string), typeof(decimal), typeof(decimal), typeof(decimal),
                                    typeof(Guid), typeof(string), typeof(string), typeof(DateTime)
                                ],
                                [
                                    [
                                        ReceiptId, "KWT-202607-0001", AcademicYearId, "2026/2027", LinkedStudentId,
                                        "Ahmad Dahlan", "2026001", Guid.NewGuid(), "VII-A", DateTime.UtcNow,
                                        "CASH", null, 350_000m, 350_000m, 0m,
                                        Guid.NewGuid(), "Kasir", null, DateTime.UtcNow
                                    ]
                                ]
                            ),
                            (
                                [
                                    "Id", "StudentChargeId", "ChargeTypeName", "BillingMonth", "AllocatedAmount",
                                    "AllocationDate", "CreatedAt"
                                ],
                                [
                                    typeof(Guid), typeof(Guid), typeof(string), typeof(string), typeof(decimal),
                                    typeof(DateTime), typeof(DateTime)
                                ],
                                []
                            )
                        ]);
                }

                return FakeDbDataReader.Empty();
            },
            OnExecuteScalar = (sql, _) =>
            {
                if (sql.Contains("FROM auth.user_students", StringComparison.OrdinalIgnoreCase))
                {
                    // Linked: return 1
                    return 1;
                }

                return null;
            }
        };

        var factory = new StubSqlConnectionFactory(() => connection);
        var handler = new GetPaymentReceiptByIdHandler(factory, parentUser);

        // Act
        var result = await handler.HandleAsync(new GetPaymentReceiptByIdQuery(ReceiptId));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(ReceiptId, result.Value!.Id);
        Assert.Equal("Ahmad Dahlan", result.Value.StudentName);
        Assert.Equal("KWT-202607-0001", result.Value.ReceiptNumber);
    }

    [Fact]
    public async Task GetPaymentReceiptByIdHandler_WhenReceiptNotFound_ReturnsNotFound()
    {
        // Arrange
        var parentUser = new MockCurrentUser(
            ParentUserId,
            "parent.hendra",
            "Hendra Dahlan",
            [RolePermissionCatalog.Roles.Parent],
            [FinPermissions.Finance.Statements.View]);

        var connection = new FakeDbConnection
        {
            OnExecuteReader = (_, _) => new FakeDbDataReader(
                [
                    (
                        ["Id"],
                        [typeof(Guid)],
                        []
                    ),
                    (
                        ["Id"],
                        [typeof(Guid)],
                        []
                    )
                ])
        };

        var factory = new StubSqlConnectionFactory(() => connection);
        var handler = new GetPaymentReceiptByIdHandler(factory, parentUser);

        // Act
        var result = await handler.HandleAsync(new GetPaymentReceiptByIdQuery(Guid.NewGuid()));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public void FinancialReportsGovernance_PermissionsAndRoles_VerifyProperAccess()
    {
        // Assert compile-time report permission constant
        Assert.Equal("Permissions.Finance.Reports.View", FinPermissions.Finance.Reports.View);
        Assert.Equal("Permissions.Finance.Payments.Record", FinPermissions.Finance.Payments.Record);

        // Assert SCHOOL_ADMIN has Reports.View but NOT Payments.Record (read-only governance)
        var adminPermissions = RolePermissionCatalog.GetPermissionsForRole(RolePermissionCatalog.Roles.SchoolAdmin);
        Assert.Contains(AuthPermissions.Finance.Reports.View, adminPermissions);
        Assert.DoesNotContain(AuthPermissions.Finance.Payments.Record, adminPermissions);

        // Assert PRINCIPAL has Reports.View but NOT Payments.Record (executive oversight)
        var principalPermissions = RolePermissionCatalog.GetPermissionsForRole(RolePermissionCatalog.Roles.Principal);
        Assert.Contains(AuthPermissions.Finance.Reports.View, principalPermissions);
        Assert.DoesNotContain(AuthPermissions.Finance.Payments.Record, principalPermissions);

        // Assert FINANCE_OFFICER has both Reports.View and Payments.Record (operational staff)
        var financeOfficerPermissions = RolePermissionCatalog.GetPermissionsForRole(RolePermissionCatalog.Roles.FinanceOfficer);
        Assert.Contains(AuthPermissions.Finance.Reports.View, financeOfficerPermissions);
        Assert.Contains(AuthPermissions.Finance.Payments.Record, financeOfficerPermissions);

        // Assert PARENT and STUDENT do not possess Reports.View
        var parentPermissions = RolePermissionCatalog.GetPermissionsForRole(RolePermissionCatalog.Roles.Parent);
        Assert.DoesNotContain(AuthPermissions.Finance.Reports.View, parentPermissions);

        var studentPermissions = RolePermissionCatalog.GetPermissionsForRole(RolePermissionCatalog.Roles.Student);
        Assert.DoesNotContain(AuthPermissions.Finance.Reports.View, studentPermissions);
    }

    #region Helper Classes

    private sealed class MockCurrentUser(
        Guid userId,
        string username,
        string fullName,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> permissions) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid UserId => userId;
        public string Username => username;
        public string FullName => fullName;
        public IReadOnlyList<string> Roles => roles;
        public IReadOnlyList<string> Permissions => permissions;
        public Guid? TeacherId => null;
        public Guid? StudentId => null;
        public Guid? GuardianId => null;
    }

    private sealed class StubStudentContract : IStudentContract
    {
        public Task<StudyGroupSummaryDto?> GetStudyGroupByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<StudyGroupSummaryDto?>(null);

        public Task<IReadOnlyList<StudentPlacementScopeDto>> GetActiveStudentsByScopeAsync(
            Guid? studyGroupId,
            string? schoolLevel,
            string? grade,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StudentPlacementScopeDto>>([]);
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

        public Func<string, FakeParameterCollection, int>? OnExecuteNonQuery { get; init; }
        public Func<string, FakeParameterCollection, object?>? OnExecuteScalar { get; init; }
        public Func<string, FakeParameterCollection, DbDataReader>? OnExecuteReader { get; init; }

        [AllowNull]
        public override string ConnectionString { get; set; } = string.Empty;
        public override string Database => "FakeDb";
        public override string DataSource => "FakeServer";
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
            public override void Commit() { }
            public override void Rollback() { }
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
                connection.OnExecuteNonQuery?.Invoke(CommandText, _parameters) ?? 1;

            public override object? ExecuteScalar() =>
                connection.OnExecuteScalar?.Invoke(CommandText, _parameters);

            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
                connection.OnExecuteReader?.Invoke(CommandText, _parameters)
                ?? FakeDbDataReader.Empty();
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
        private readonly List<DbParameter> _items = [];
        public override int Count => _items.Count;
        public override object SyncRoot => _items;
        public override int Add(object value) { _items.Add((DbParameter)value); return _items.Count - 1; }
        public override void AddRange(Array values) { foreach (var v in values) _items.Add((DbParameter)v!); }
        public override void Clear() => _items.Clear();
        public override bool Contains(object value) => _items.Contains((DbParameter)value);
        public override bool Contains(string value) => _items.Any(p => p.ParameterName == value);
        public override void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
        public override IEnumerator GetEnumerator() => _items.GetEnumerator();
        public override int IndexOf(object value) => _items.IndexOf((DbParameter)value);
        public override int IndexOf(string parameterName) => _items.FindIndex(p => p.ParameterName == parameterName);
        public override void Insert(int index, object value) => _items.Insert(index, (DbParameter)value);
        public override void Remove(object value) => _items.Remove((DbParameter)value);
        public override void RemoveAt(int index) => _items.RemoveAt(index);
        public override void RemoveAt(string parameterName) => _items.RemoveAll(p => p.ParameterName == parameterName);
        protected override DbParameter GetParameter(int index) => _items[index];
        protected override DbParameter GetParameter(string parameterName) => _items.First(p => p.ParameterName == parameterName);
        protected override void SetParameter(int index, DbParameter value) => _items[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value)
        {
            var idx = IndexOf(parameterName);
            if (idx >= 0) _items[idx] = value;
            else _items.Add(value);
        }
    }

    private sealed class FakeDbDataReader : DbDataReader
    {
        private readonly List<(string[] columns, Type[] fieldTypes, object?[][] rows)> _resultSets;
        private int _resultSetIndex;
        private int _rowIndex = -1;

        public FakeDbDataReader(List<(string[] columns, Type[] fieldTypes, object?[][] rows)> resultSets)
        {
            _resultSets = resultSets.Count > 0 ? resultSets : [([], [], [])];
            _resultSetIndex = 0;
        }

        public static FakeDbDataReader SingleResult(string[] columns, Type[] fieldTypes, object?[][] rows) =>
            new([(columns, fieldTypes, rows)]);

        public static FakeDbDataReader Empty() =>
            new([([], [], [])]);

        private (string[] columns, Type[] fieldTypes, object?[][] rows) CurrentResult =>
            _resultSets[_resultSetIndex];

        public override int FieldCount => CurrentResult.columns.Length;
        public override bool HasRows => CurrentResult.rows.Length > 0;
        public override bool IsClosed => false;
        public override int RecordsAffected => 0;
        public override int Depth => 0;
        public override object this[int ordinal] => GetValue(ordinal);
        public override object this[string name] => GetValue(GetOrdinal(name));

        public override bool Read() => ++_rowIndex < CurrentResult.rows.Length;

        public override bool NextResult()
        {
            _resultSetIndex++;
            _rowIndex = -1;
            return _resultSetIndex < _resultSets.Count;
        }

        public override string GetName(int ordinal) => CurrentResult.columns[ordinal];
        public override int GetOrdinal(string name) => Array.FindIndex(CurrentResult.columns, c => string.Equals(c, name, StringComparison.OrdinalIgnoreCase));
        public override object GetValue(int ordinal) => CurrentResult.rows[_rowIndex][ordinal] ?? DBNull.Value;
        public override bool IsDBNull(int ordinal) => CurrentResult.rows[_rowIndex][ordinal] is null or DBNull;
        public override Type GetFieldType(int ordinal) => CurrentResult.fieldTypes[ordinal];
        public override string GetDataTypeName(int ordinal) => GetFieldType(ordinal).Name;
        public override IEnumerator GetEnumerator() => CurrentResult.rows.GetEnumerator();

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

    #endregion
}
