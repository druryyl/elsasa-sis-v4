using ElsasaSis.Modules.Auth;
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
    public void SchoolAdmin_ReceivesAllStage1AdminPermissions()
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
            Permissions.Teaching.Assignments.Oversight
        ];

        var actual = RolePermissionCatalog.GetPermissionsForRole(RolePermissionCatalog.Roles.SchoolAdmin);

        Assert.Equal(expected.OrderBy(p => p, StringComparer.Ordinal), actual.OrderBy(p => p, StringComparer.Ordinal));
        Assert.DoesNotContain(Permissions.Students.Admissions.Approve, actual);
        Assert.DoesNotContain(Permissions.Students.Roster.ViewOwn, actual);
    }

    [Fact]
    public void Principal_ReceivesExpectedStage1Permissions()
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
            Permissions.Teaching.Assignments.Oversight
        ];

        var actual = RolePermissionCatalog.GetPermissionsForRole(RolePermissionCatalog.Roles.Principal);

        Assert.Equal(expected.OrderBy(p => p, StringComparer.Ordinal), actual.OrderBy(p => p, StringComparer.Ordinal));
    }

    [Fact]
    public void Teacher_ReceivesExpectedStage1Permissions()
    {
        string[] expected =
        [
            Permissions.Operations.Calendar.View,
            Permissions.Operations.Announcements.View,
            Permissions.Teaching.Assignments.View
        ];

        var actual = RolePermissionCatalog.GetPermissionsForRole(RolePermissionCatalog.Roles.Teacher);

        Assert.Equal(expected.OrderBy(p => p, StringComparer.Ordinal), actual.OrderBy(p => p, StringComparer.Ordinal));
    }

    [Fact]
    public void HomeroomTeacher_ReceivesExpectedStage1Permissions()
    {
        string[] expected =
        [
            Permissions.Operations.Calendar.View,
            Permissions.Operations.Announcements.View,
            Permissions.Students.Roster.ViewOwn
        ];

        var actual = RolePermissionCatalog.GetPermissionsForRole(RolePermissionCatalog.Roles.HomeroomTeacher);

        Assert.Equal(expected.OrderBy(p => p, StringComparer.Ordinal), actual.OrderBy(p => p, StringComparer.Ordinal));
    }

    [Fact]
    public void FinanceOfficer_ReceivesExpectedStage1Permissions()
    {
        string[] expected =
        [
            Permissions.Operations.Calendar.View,
            Permissions.Operations.Announcements.View,
            Permissions.Students.Directory.View
        ];

        var actual = RolePermissionCatalog.GetPermissionsForRole(RolePermissionCatalog.Roles.FinanceOfficer);

        Assert.Equal(expected.OrderBy(p => p, StringComparer.Ordinal), actual.OrderBy(p => p, StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("PARENT")]
    [InlineData("STUDENT")]
    public void Stage1DeferredRoles_ReturnEmptyPermissions(string roleCode)
    {
        var permissions = RolePermissionCatalog.GetPermissionsForRole(roleCode);

        Assert.Empty(permissions);
    }

    [Fact]
    public void GetPermissionsForRoles_DeduplicatesAndOrdersPermissions()
    {
        var adminAndPrincipal = RolePermissionCatalog.GetPermissionsForRoles([
            RolePermissionCatalog.Roles.SchoolAdmin,
            RolePermissionCatalog.Roles.Principal
        ]);

        Assert.Equal(20, adminAndPrincipal.Count);
        Assert.Equal(
            adminAndPrincipal.OrderBy(p => p, StringComparer.Ordinal),
            adminAndPrincipal);
        Assert.Contains(Permissions.Operations.Calendar.Manage, adminAndPrincipal);
        Assert.Contains(Permissions.Students.Admissions.Approve, adminAndPrincipal);

        var teacherAndHomeroom = RolePermissionCatalog.GetPermissionsForRoles([
            RolePermissionCatalog.Roles.Teacher,
            RolePermissionCatalog.Roles.HomeroomTeacher
        ]);

        string[] expectedCombined =
        [
            Permissions.Operations.Announcements.View,
            Permissions.Operations.Calendar.View,
            Permissions.Students.Roster.ViewOwn,
            Permissions.Teaching.Assignments.View
        ];

        Assert.Equal(expectedCombined, teacherAndHomeroom);
    }

    [Fact]
    public void ModulePermissionsAndPolicies_MatchAuthCatalogStrings()
    {
        Assert.Equal(21, Permissions.All.Count);
        Assert.Equal(Permissions.All.Distinct(StringComparer.Ordinal).Count(), Permissions.All.Count);

        Assert.Equal(Permissions.Operations.Calendar.View, OprPermissions.Operations.Calendar.View);
        Assert.Equal(Permissions.Operations.Calendar.Manage, OprPermissions.Operations.Calendar.Manage);
        Assert.Equal(Permissions.Operations.Rooms.View, OprPermissions.Operations.Rooms.View);
        Assert.Equal(Permissions.Operations.Rooms.Manage, OprPermissions.Operations.Rooms.Manage);
        Assert.Equal(Permissions.Operations.Announcements.View, OprPermissions.Operations.Announcements.View);
        Assert.Equal(Permissions.Operations.Announcements.Create, OprPermissions.Operations.Announcements.Create);
        Assert.Equal(Permissions.Operations.Announcements.Publish, OprPermissions.Operations.Announcements.Publish);

        Assert.Equal(Permissions.Students.Admissions.View, StdPermissions.Students.Admissions.View);
        Assert.Equal(Permissions.Students.Admissions.Register, StdPermissions.Students.Admissions.Register);
        Assert.Equal(Permissions.Students.Admissions.Decide, StdPermissions.Students.Admissions.Decide);
        Assert.Equal(Permissions.Students.Admissions.Approve, StdPermissions.Students.Admissions.Approve);
        Assert.Equal(Permissions.Students.Admissions.Enroll, StdPermissions.Students.Admissions.Enroll);
        Assert.Equal(Permissions.Students.Directory.View, StdPermissions.Students.Directory.View);
        Assert.Equal(Permissions.Students.Directory.Manage, StdPermissions.Students.Directory.Manage);
        Assert.Equal(Permissions.Students.Placements.Manage, StdPermissions.Students.Placements.Manage);
        Assert.Equal(Permissions.Students.Roster.ViewOwn, StdPermissions.Students.Roster.ViewOwn);

        Assert.Equal(Permissions.Teaching.Directory.Manage, TchPermissions.Teaching.Directory.Manage);
        Assert.Equal(Permissions.Teaching.Capabilities.Manage, TchPermissions.Teaching.Capabilities.Manage);
        Assert.Equal(Permissions.Teaching.Assignments.View, TchPermissions.Teaching.Assignments.View);
        Assert.Equal(Permissions.Teaching.Assignments.Manage, TchPermissions.Teaching.Assignments.Manage);
        Assert.Equal(Permissions.Teaching.Assignments.Oversight, TchPermissions.Teaching.Assignments.Oversight);

        Assert.Equal(Policies.Operations.Calendar.ViewOrManage, OprPolicies.Operations.Calendar.ViewOrManage);
        Assert.Equal(Policies.Operations.Rooms.ViewOrManage, OprPolicies.Operations.Rooms.ViewOrManage);
        Assert.Equal(Policies.Operations.Announcements.ViewOrManage, OprPolicies.Operations.Announcements.ViewOrManage);
        Assert.Equal(Policies.Students.Admissions.DecideOrApprove, StdPolicies.Students.Admissions.DecideOrApprove);
        Assert.Equal(Policies.Students.DirectoryOrOwnRoster, StdPolicies.Students.DirectoryOrOwnRoster);
        Assert.Equal(Policies.Teaching.Assignments.ViewOrOversight, TchPolicies.Teaching.Assignments.ViewOrOversight);
    }

    [Fact]
    public void UnknownOrEmptyRole_ReturnsEmptyPermissions()
    {
        Assert.Empty(RolePermissionCatalog.GetPermissionsForRole("UNKNOWN_ROLE"));
        Assert.Empty(RolePermissionCatalog.GetPermissionsForRole("   "));
    }
}
