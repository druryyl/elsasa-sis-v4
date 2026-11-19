namespace ElsasaSis.Modules.Auth;

public static class Permissions
{
    public static class Operations
    {
        public static class Calendar
        {
            public const string View = "Permissions.Operations.Calendar.View";
            public const string Manage = "Permissions.Operations.Calendar.Manage";
        }
    }

    public static IReadOnlyList<string> All { get; } =
    [
        Operations.Calendar.View,
        Operations.Calendar.Manage
    ];
}
