using ElsasaSis.Modules.Auth;

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

    [Theory]
    [InlineData("FINANCE_OFFICER")]
    [InlineData("TEACHER")]
    [InlineData("HOMEROOM_TEACHER")]
    [InlineData("PARENT")]
    [InlineData("STUDENT")]
    public void Stage0NonCalendarRoles_ReturnEmptyPermissions(string roleCode)
    {
        var permissions = RolePermissionCatalog.GetPermissionsForRole(roleCode);

        Assert.Empty(permissions);
    }

    [Fact]
    public void GetPermissionsForRoles_DeduplicatesAndOrdersPermissions()
    {
        var permissions = RolePermissionCatalog.GetPermissionsForRoles([
            RolePermissionCatalog.Roles.SchoolAdmin,
            RolePermissionCatalog.Roles.Principal
        ]);

        Assert.Equal(2, permissions.Count);
        Assert.Equal(Permissions.Operations.Calendar.Manage, permissions[0]);
        Assert.Equal(Permissions.Operations.Calendar.View, permissions[1]);
    }

    [Fact]
    public void UnknownOrEmptyRole_ReturnsEmptyPermissions()
    {
        Assert.Empty(RolePermissionCatalog.GetPermissionsForRole("UNKNOWN_ROLE"));
        Assert.Empty(RolePermissionCatalog.GetPermissionsForRole("   "));
    }
}
