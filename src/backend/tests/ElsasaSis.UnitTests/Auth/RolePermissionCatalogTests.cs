using ElsasaSis.Modules.Auth;
using AcdPermissions = ElsasaSis.Modules.Acd.Permissions;
using AcdPolicies = ElsasaSis.Modules.Acd.Policies;
using DevPermissions = ElsasaSis.Modules.Dev.Permissions;
using DevPolicies = ElsasaSis.Modules.Dev.Policies;
using FinPermissions = ElsasaSis.Modules.Fin.Permissions;
using FinPolicies = ElsasaSis.Modules.Fin.Policies;
using OprPermissions = ElsasaSis.Modules.Opr.Permissions;
using OprPolicies = ElsasaSis.Modules.Opr.Policies;
using StdPermissions = ElsasaSis.Modules.Std.Permissions;
using StdPolicies = ElsasaSis.Modules.Std.Policies;
using TchPermissions = ElsasaSis.Modules.Tch.Permissions;
using TchPolicies = ElsasaSis.Modules.Tch.Policies;

namespace ElsasaSis.UnitTests.Auth;

public sealed class RolePermissionCatalogTests
{
    [Fact]
    public void SchoolAdmin_ReceivesCalendarViewAndManagePermissions()
    {
        var permissions = RolePermissionCatalog.GetPermissionsForRole(RolePermissionCatalog.Roles.SchoolAdmin);

        Assert.Contains(Permissions.Operations.Calendar.View, permissions);
        Assert.Contains(Permissions.Operations.Calendar.Manage, permissions);
    }

    [Fact]
    public void Principal_ReceivesCalendarViewOnly()
    {
        var permissions = RolePermissionCatalog.GetPermissionsForRole(RolePermissionCatalog.Roles.Principal);

        Assert.Contains(Permissions.Operations.Calendar.View, permissions);
        Assert.DoesNotContain(Permissions.Operations.Calendar.Manage, permissions);
    }

    [Fact]
    public void SchoolAdmin_ReceivesAllStage3AdminPermissions()
    {
        string[] expected =
        [
            Permissions.Operations.Calendar.View,
            Permissions.Operations.Calendar.Manage,
            Permissions.Operations.Rooms.View,
            Permissions.Operations.Rooms.Manage,
            Permissions.Operations.Announcements.View,
            Permissions.Operations.Announcements.Create,
            Permissions.Operations.Announcements.Publish,
            Permissions.Students.Admissions.View,
            Permissions.Students.Admissions.Register,
            Permissions.Students.Admissions.Decide,
            Permissions.Students.Admissions.Enroll,
            Permissions.Students.Directory.View,
            Permissions.Students.Directory.Manage,
            Permissions.Students.Placements.Manage,
            Permissions.Teaching.Directory.Manage,
            Permissions.Teaching.Capabilities.Manage,
            Permissions.Teaching.Assignments.View,
            Permissions.Teaching.Assignments.Manage,
            Permissions.Teaching.Assignments.Oversight,
            Permissions.Operations.Timetables.View,
            Permissions.Operations.Timetables.Manage,
            Permissions.Operations.Timetables.ViewOwn,
            Permissions.Academic.Curriculum.View,
            Permissions.Academic.Curriculum.Manage,
            Permissions.Academic.Attendance.View,
            Permissions.Academic.Attendance.Record,
            Permissions.Academic.Scorebook.View,
            Permissions.Academic.Scorebook.Manage,
            Permissions.Academic.ReportCards.View,
            Permissions.Academic.ReportCards.Generate,
            Permissions.Academic.ReportCards.ProvideNotes,
            Permissions.Academic.ReportCards.Remarks,
            Permissions.Academic.ReportCards.Finalize,
            Permissions.Academic.ReportCards.Override,
            Permissions.Academic.Monitoring.View,
            Permissions.Development.Extracurricular.Manage,
            Permissions.Development.Discipline.Record,
            Permissions.Development.Achievements.Record,
            Permissions.Development.Dossier.View,
            Permissions.Finance.FeePolicy.Manage,
            Permissions.Finance.Discounts.Manage,
            Permissions.Finance.Billing.Generate,
            Permissions.Finance.Billing.ManageAdHoc,
            Permissions.Finance.Billing.Cancel,
            Permissions.Finance.Reports.View
        ];

        var actual = RolePermissionCatalog.GetPermissionsForRole(RolePermissionCatalog.Roles.SchoolAdmin);

        Assert.Equal(expected.OrderBy(p => p, StringComparer.Ordinal), actual.OrderBy(p => p, StringComparer.Ordinal));
        Assert.DoesNotContain(Permissions.Students.Admissions.Approve, actual);
        Assert.DoesNotContain(Permissions.Students.Roster.ViewOwn, actual);
    }

    [Fact]
    public void Principal_ReceivesExpectedStage3Permissions()
    {
        string[] expected =
        [
            Permissions.Operations.Calendar.View,
            Permissions.Operations.Rooms.View,
            Permissions.Operations.Announcements.View,
            Permissions.Students.Admissions.View,
            Permissions.Students.Admissions.Approve,
            Permissions.Students.Directory.View,
            Permissions.Teaching.Assignments.View,
            Permissions.Teaching.Assignments.Oversight,
            Permissions.Operations.Timetables.View,
            Permissions.Academic.Curriculum.View,
            Permissions.Academic.Attendance.View,
            Permissions.Academic.Scorebook.View,
            Permissions.Academic.ReportCards.View,
            Permissions.Academic.Monitoring.View,
            Permissions.Development.Discipline.Escalate,
            Permissions.Development.Dossier.View,
            Permissions.Finance.Reports.View
        ];

        var actual = RolePermissionCatalog.GetPermissionsForRole(RolePermissionCatalog.Roles.Principal);

        Assert.Equal(expected.OrderBy(p => p, StringComparer.Ordinal), actual.OrderBy(p => p, StringComparer.Ordinal));
    }

    [Fact]
    public void Teacher_ReceivesExpectedStage3Permissions()
    {
        string[] expected =
        [
            Permissions.Operations.Calendar.View,
            Permissions.Operations.Announcements.View,
            Permissions.Teaching.Assignments.View,
            Permissions.Operations.Timetables.ViewOwn,
            Permissions.Academic.Curriculum.View,
            Permissions.Academic.Attendance.View,
            Permissions.Academic.Attendance.Record,
            Permissions.Academic.Scorebook.View,
            Permissions.Academic.Scorebook.Manage,
            Permissions.Academic.ReportCards.ProvideNotes,
            Permissions.Development.Behaviour.Record,
            Permissions.Development.Discipline.Record,
            Permissions.Development.Achievements.Record,
            Permissions.Development.Extracurricular.Log,
            Permissions.Development.Dossier.View
        ];

        var actual = RolePermissionCatalog.GetPermissionsForRole(RolePermissionCatalog.Roles.Teacher);

        Assert.Equal(expected.OrderBy(p => p, StringComparer.Ordinal), actual.OrderBy(p => p, StringComparer.Ordinal));
    }

    [Fact]
    public void HomeroomTeacher_ReceivesExpectedStage3Permissions()
    {
        string[] expected =
        [
            Permissions.Operations.Calendar.View,
            Permissions.Operations.Announcements.View,
            Permissions.Teaching.Assignments.View,
            Permissions.Operations.Timetables.ViewOwn,
            Permissions.Academic.Curriculum.View,
            Permissions.Academic.Attendance.View,
            Permissions.Academic.Attendance.Record,
            Permissions.Academic.Scorebook.View,
            Permissions.Academic.Scorebook.Manage,
            Permissions.Students.Roster.ViewOwn,
            Permissions.Academic.ReportCards.ProvideNotes,
            Permissions.Academic.ReportCards.Remarks,
            Permissions.Academic.ReportCards.Finalize,
            Permissions.Development.Behaviour.Record,
            Permissions.Development.Discipline.Record,
            Permissions.Development.Achievements.Record,
            Permissions.Development.Extracurricular.Log,
            Permissions.Development.Dossier.View
        ];

        var actual = RolePermissionCatalog.GetPermissionsForRole(RolePermissionCatalog.Roles.HomeroomTeacher);

        Assert.Equal(expected.OrderBy(p => p, StringComparer.Ordinal), actual.OrderBy(p => p, StringComparer.Ordinal));
    }

    [Fact]
    public void FinanceOfficer_ReceivesExpectedStage3Permissions()
    {
        string[] expected =
        [
            Permissions.Operations.Calendar.View,
            Permissions.Operations.Announcements.View,
            Permissions.Students.Directory.View,
            Permissions.Finance.FeePolicy.Manage,
            Permissions.Finance.Discounts.Manage,
            Permissions.Finance.Billing.Generate,
            Permissions.Finance.Billing.ManageAdHoc,
            Permissions.Finance.Billing.Cancel,
            Permissions.Finance.Payments.Record,
            Permissions.Finance.Deposits.Allocate,
            Permissions.Finance.Statements.View,
            Permissions.Finance.Reports.View
        ];

        var actual = RolePermissionCatalog.GetPermissionsForRole(RolePermissionCatalog.Roles.FinanceOfficer);

        Assert.Equal(expected.OrderBy(p => p, StringComparer.Ordinal), actual.OrderBy(p => p, StringComparer.Ordinal));
    }

    [Fact]
    public void Parent_PossessesExactlyFiveAssignedPermissions()
    {
        string[] expected =
        [
            Permissions.Operations.Calendar.View,
            Permissions.Operations.Announcements.View,
            Permissions.Students.Children.View,
            Permissions.Academic.ReportCards.View,
            Permissions.Finance.Statements.View
        ];

        var actual = RolePermissionCatalog.GetPermissionsForRole(RolePermissionCatalog.Roles.Parent);

        Assert.Equal(5, actual.Count);
        Assert.Equal(expected.OrderBy(p => p, StringComparer.Ordinal), actual.OrderBy(p => p, StringComparer.Ordinal));
    }

    [Fact]
    public void Student_PossessesExactlyThreeAssignedPermissions()
    {
        string[] expected =
        [
            Permissions.Operations.Timetables.ViewOwn,
            Permissions.Operations.Calendar.View,
            Permissions.Operations.Announcements.View
        ];

        var actual = RolePermissionCatalog.GetPermissionsForRole(RolePermissionCatalog.Roles.Student);

        Assert.Equal(3, actual.Count);
        Assert.Equal(expected.OrderBy(p => p, StringComparer.Ordinal), actual.OrderBy(p => p, StringComparer.Ordinal));
    }

    [Fact]
    public void PrincipalAndSchoolAdmin_PossessAcademicMonitoringViewPermission()
    {
        var adminPermissions = RolePermissionCatalog.GetPermissionsForRole(RolePermissionCatalog.Roles.SchoolAdmin);
        var principalPermissions = RolePermissionCatalog.GetPermissionsForRole(RolePermissionCatalog.Roles.Principal);

        Assert.Contains(Permissions.Academic.Monitoring.View, adminPermissions);
        Assert.Contains(Permissions.Academic.Monitoring.View, principalPermissions);
    }

    [Fact]
    public void GetPermissionsForRoles_DeduplicatesAndOrdersPermissions()
    {
        var adminAndPrincipal = RolePermissionCatalog.GetPermissionsForRoles([
            RolePermissionCatalog.Roles.SchoolAdmin,
            RolePermissionCatalog.Roles.Principal
        ]);

        Assert.Equal(47, adminAndPrincipal.Count);
        Assert.Equal(
            adminAndPrincipal.OrderBy(p => p, StringComparer.Ordinal),
            adminAndPrincipal);
        Assert.Contains(Permissions.Academic.Monitoring.View, adminAndPrincipal);
        Assert.Contains(Permissions.Operations.Calendar.Manage, adminAndPrincipal);
        Assert.Contains(Permissions.Students.Admissions.Approve, adminAndPrincipal);
        Assert.Contains(Permissions.Operations.Timetables.Manage, adminAndPrincipal);
        Assert.Contains(Permissions.Academic.Curriculum.Manage, adminAndPrincipal);
        Assert.Contains(Permissions.Academic.ReportCards.Generate, adminAndPrincipal);
        Assert.Contains(Permissions.Development.Discipline.Escalate, adminAndPrincipal);
        Assert.Contains(Permissions.Finance.FeePolicy.Manage, adminAndPrincipal);

        var teacherAndHomeroom = RolePermissionCatalog.GetPermissionsForRoles([
            RolePermissionCatalog.Roles.Teacher,
            RolePermissionCatalog.Roles.HomeroomTeacher
        ]);

        string[] expectedCombined =
        [
            Permissions.Academic.Attendance.Record,
            Permissions.Academic.Attendance.View,
            Permissions.Academic.Curriculum.View,
            Permissions.Academic.ReportCards.Finalize,
            Permissions.Academic.ReportCards.ProvideNotes,
            Permissions.Academic.ReportCards.Remarks,
            Permissions.Academic.Scorebook.Manage,
            Permissions.Academic.Scorebook.View,
            Permissions.Development.Achievements.Record,
            Permissions.Development.Behaviour.Record,
            Permissions.Development.Discipline.Record,
            Permissions.Development.Dossier.View,
            Permissions.Development.Extracurricular.Log,
            Permissions.Operations.Announcements.View,
            Permissions.Operations.Calendar.View,
            Permissions.Operations.Timetables.ViewOwn,
            Permissions.Students.Roster.ViewOwn,
            Permissions.Teaching.Assignments.View
        ];

        Assert.Equal(expectedCombined, teacherAndHomeroom);
    }

    [Fact]
    public void ModulePermissionsAndPolicies_MatchAuthCatalogStrings()
    {
        Assert.Equal(54, Permissions.All.Count);
        Assert.Equal(Permissions.All.Distinct(StringComparer.Ordinal).Count(), Permissions.All.Count);

        Assert.Equal(Permissions.Operations.Calendar.View, OprPermissions.Operations.Calendar.View);
        Assert.Equal(Permissions.Operations.Calendar.Manage, OprPermissions.Operations.Calendar.Manage);
        Assert.Equal(Permissions.Operations.Rooms.View, OprPermissions.Operations.Rooms.View);
        Assert.Equal(Permissions.Operations.Rooms.Manage, OprPermissions.Operations.Rooms.Manage);
        Assert.Equal(Permissions.Operations.Announcements.View, OprPermissions.Operations.Announcements.View);
        Assert.Equal(Permissions.Operations.Announcements.Create, OprPermissions.Operations.Announcements.Create);
        Assert.Equal(Permissions.Operations.Announcements.Publish, OprPermissions.Operations.Announcements.Publish);
        Assert.Equal(Permissions.Operations.Timetables.View, OprPermissions.Operations.Timetables.View);
        Assert.Equal(Permissions.Operations.Timetables.Manage, OprPermissions.Operations.Timetables.Manage);
        Assert.Equal(Permissions.Operations.Timetables.ViewOwn, OprPermissions.Operations.Timetables.ViewOwn);

        Assert.Equal(Permissions.Students.Admissions.View, StdPermissions.Students.Admissions.View);
        Assert.Equal(Permissions.Students.Admissions.Register, StdPermissions.Students.Admissions.Register);
        Assert.Equal(Permissions.Students.Admissions.Decide, StdPermissions.Students.Admissions.Decide);
        Assert.Equal(Permissions.Students.Admissions.Approve, StdPermissions.Students.Admissions.Approve);
        Assert.Equal(Permissions.Students.Admissions.Enroll, StdPermissions.Students.Admissions.Enroll);
        Assert.Equal(Permissions.Students.Directory.View, StdPermissions.Students.Directory.View);
        Assert.Equal(Permissions.Students.Directory.Manage, StdPermissions.Students.Directory.Manage);
        Assert.Equal(Permissions.Students.Placements.Manage, StdPermissions.Students.Placements.Manage);
        Assert.Equal(Permissions.Students.Roster.ViewOwn, StdPermissions.Students.Roster.ViewOwn);
        Assert.Equal(Permissions.Students.Children.View, StdPermissions.Students.Children.View);

        Assert.Equal(Permissions.Teaching.Directory.Manage, TchPermissions.Teaching.Directory.Manage);
        Assert.Equal(Permissions.Teaching.Capabilities.Manage, TchPermissions.Teaching.Capabilities.Manage);
        Assert.Equal(Permissions.Teaching.Assignments.View, TchPermissions.Teaching.Assignments.View);
        Assert.Equal(Permissions.Teaching.Assignments.Manage, TchPermissions.Teaching.Assignments.Manage);
        Assert.Equal(Permissions.Teaching.Assignments.Oversight, TchPermissions.Teaching.Assignments.Oversight);

        Assert.Equal(Permissions.Academic.Curriculum.View, AcdPermissions.Academic.Curriculum.View);
        Assert.Equal(Permissions.Academic.Curriculum.Manage, AcdPermissions.Academic.Curriculum.Manage);
        Assert.Equal(Permissions.Academic.Attendance.View, AcdPermissions.Academic.Attendance.View);
        Assert.Equal(Permissions.Academic.Attendance.Record, AcdPermissions.Academic.Attendance.Record);
        Assert.Equal(Permissions.Academic.Scorebook.View, AcdPermissions.Academic.Scorebook.View);
        Assert.Equal(Permissions.Academic.Scorebook.Manage, AcdPermissions.Academic.Scorebook.Manage);
        Assert.Equal(Permissions.Academic.ReportCards.View, AcdPermissions.Academic.ReportCards.View);
        Assert.Equal(Permissions.Academic.ReportCards.Generate, AcdPermissions.Academic.ReportCards.Generate);
        Assert.Equal(Permissions.Academic.ReportCards.ProvideNotes, AcdPermissions.Academic.ReportCards.ProvideNotes);
        Assert.Equal(Permissions.Academic.ReportCards.Remarks, AcdPermissions.Academic.ReportCards.Remarks);
        Assert.Equal(Permissions.Academic.ReportCards.Finalize, AcdPermissions.Academic.ReportCards.Finalize);
        Assert.Equal(Permissions.Academic.ReportCards.Override, AcdPermissions.Academic.ReportCards.Override);
        Assert.Equal(Permissions.Academic.Monitoring.View, AcdPermissions.Academic.Monitoring.View);

        Assert.Equal(Permissions.Development.Behaviour.Record, DevPermissions.Development.Behaviour.Record);
        Assert.Equal(Permissions.Development.Discipline.Record, DevPermissions.Development.Discipline.Record);
        Assert.Equal(Permissions.Development.Discipline.Escalate, DevPermissions.Development.Discipline.Escalate);
        Assert.Equal(Permissions.Development.Achievements.Record, DevPermissions.Development.Achievements.Record);
        Assert.Equal(Permissions.Development.Extracurricular.Manage, DevPermissions.Development.Extracurricular.Manage);
        Assert.Equal(Permissions.Development.Extracurricular.Log, DevPermissions.Development.Extracurricular.Log);
        Assert.Equal(Permissions.Development.Dossier.View, DevPermissions.Development.Dossier.View);

        Assert.Equal(Permissions.Finance.FeePolicy.Manage, FinPermissions.Finance.FeePolicy.Manage);
        Assert.Equal(Permissions.Finance.Discounts.Manage, FinPermissions.Finance.Discounts.Manage);
        Assert.Equal(Permissions.Finance.Billing.Generate, FinPermissions.Finance.Billing.Generate);
        Assert.Equal(Permissions.Finance.Billing.ManageAdHoc, FinPermissions.Finance.Billing.ManageAdHoc);
        Assert.Equal(Permissions.Finance.Billing.Cancel, FinPermissions.Finance.Billing.Cancel);
        Assert.Equal(Permissions.Finance.Payments.Record, FinPermissions.Finance.Payments.Record);
        Assert.Equal(Permissions.Finance.Deposits.Allocate, FinPermissions.Finance.Deposits.Allocate);
        Assert.Equal(Permissions.Finance.Statements.View, FinPermissions.Finance.Statements.View);
        Assert.Equal(Permissions.Finance.Reports.View, FinPermissions.Finance.Reports.View);

        Assert.Equal(Policies.Operations.Calendar.ViewOrManage, OprPolicies.Operations.Calendar.ViewOrManage);
        Assert.Equal(Policies.Operations.Rooms.ViewOrManage, OprPolicies.Operations.Rooms.ViewOrManage);
        Assert.Equal(Policies.Operations.Announcements.ViewOrManage, OprPolicies.Operations.Announcements.ViewOrManage);
        Assert.Equal(Policies.Operations.Timetables.ViewOrManageOrOwn, OprPolicies.Operations.Timetables.ViewOrManageOrOwn);

        Assert.Equal(Policies.Students.Admissions.DecideOrApprove, StdPolicies.Students.Admissions.DecideOrApprove);
        Assert.Equal(Policies.Students.DirectoryOrOwnRoster, StdPolicies.Students.DirectoryOrOwnRoster);
        Assert.Equal(Policies.Teaching.Assignments.ViewOrOversight, TchPolicies.Teaching.Assignments.ViewOrOversight);

        Assert.Equal(Policies.Academic.Curriculum.ViewOrManage, AcdPolicies.Academic.Curriculum.ViewOrManage);
        Assert.Equal(Policies.Academic.Attendance.ViewOrRecord, AcdPolicies.Academic.Attendance.ViewOrRecord);
        Assert.Equal(Policies.Academic.Scorebook.ViewOrManage, AcdPolicies.Academic.Scorebook.ViewOrManage);
        Assert.Equal(Policies.Academic.ReportCards.ViewOrManage, AcdPolicies.Academic.ReportCards.ViewOrManage);

        Assert.Equal(Policies.Development.Pastoral.RecordOrView, DevPolicies.Development.Pastoral.RecordOrView);

        Assert.Equal(Policies.Finance.Cashier.Manage, FinPolicies.Finance.Cashier.Manage);
        Assert.Equal(Policies.Finance.Reports.View, FinPolicies.Finance.Reports.View);
    }

    [Fact]
    public void UnknownOrEmptyRole_ReturnsEmptyPermissions()
    {
        Assert.Empty(RolePermissionCatalog.GetPermissionsForRole("UNKNOWN_ROLE"));
        Assert.Empty(RolePermissionCatalog.GetPermissionsForRole("   "));
    }
}
