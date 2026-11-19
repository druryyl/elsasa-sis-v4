namespace ElsasaSis.Modules.Tch;

public static class Permissions
{
    public static class Teaching
    {
        public static class Directory
        {
            public const string Manage = "Permissions.Teaching.Directory.Manage";
        }

        public static class Capabilities
        {
            public const string Manage = "Permissions.Teaching.Capabilities.Manage";
        }

        public static class Assignments
        {
            public const string View = "Permissions.Teaching.Assignments.View";
            public const string Manage = "Permissions.Teaching.Assignments.Manage";
            public const string Oversight = "Permissions.Teaching.Assignments.Oversight";
        }
    }
}

public static class Policies
{
    public static class Teaching
    {
        public static class Assignments
        {
            public const string ViewOrOversight = "Policies.Teaching.Assignments.ViewOrOversight";
        }
    }
}
