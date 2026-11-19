namespace ElsasaSis.Modules.Auth;

public static class RolePermissionCatalog
{
    public static class Roles
    {
        public const string SchoolAdmin = "SCHOOL_ADMIN";
        public const string FinanceOfficer = "FINANCE_OFFICER";
        public const string Teacher = "TEACHER";
        public const string HomeroomTeacher = "HOMEROOM_TEACHER";
        public const string Principal = "PRINCIPAL";
        public const string Parent = "PARENT";
        public const string Student = "STUDENT";

        public static IReadOnlyList<string> All { get; } =
        [
            SchoolAdmin,
            FinanceOfficer,
            Teacher,
            HomeroomTeacher,
            Principal,
            Parent,
            Student
        ];
    }

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> RolePermissions =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [Roles.SchoolAdmin] =
            [
                Permissions.Operations.Calendar.View,
                Permissions.Operations.Calendar.Manage
            ],
            [Roles.Principal] =
            [
                Permissions.Operations.Calendar.View
            ],
            [Roles.FinanceOfficer] = [],
            [Roles.Teacher] = [],
            [Roles.HomeroomTeacher] = [],
            [Roles.Parent] = [],
            [Roles.Student] = []
        };

    public static IReadOnlyList<string> GetPermissionsForRole(string roleCode)
    {
        if (string.IsNullOrWhiteSpace(roleCode))
        {
            return [];
        }

        return RolePermissions.TryGetValue(roleCode.Trim(), out var permissions)
            ? permissions
            : [];
    }

    public static IReadOnlyList<string> GetPermissionsForRoles(IEnumerable<string> roleCodes)
    {
        ArgumentNullException.ThrowIfNull(roleCodes);

        return roleCodes
            .SelectMany(GetPermissionsForRole)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();
    }
}
