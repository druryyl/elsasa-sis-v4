namespace ElsasaSis.Modules.Acd;

public static class Permissions
{
    public static class Academic
    {
        public static class Curriculum
        {
            public const string View = "Permissions.Academic.Curriculum.View";
            public const string Manage = "Permissions.Academic.Curriculum.Manage";
        }

        public static class Attendance
        {
            public const string View = "Permissions.Academic.Attendance.View";
            public const string Record = "Permissions.Academic.Attendance.Record";
        }

        public static class Scorebook
        {
            public const string View = "Permissions.Academic.Scorebook.View";
            public const string Manage = "Permissions.Academic.Scorebook.Manage";
        }
    }
}

public static class Policies
{
    public static class Academic
    {
        public static class Curriculum
        {
            public const string ViewOrManage = "Policies.Academic.Curriculum.ViewOrManage";
        }

        public static class Attendance
        {
            public const string ViewOrRecord = "Policies.Academic.Attendance.ViewOrRecord";
        }

        public static class Scorebook
        {
            public const string ViewOrManage = "Policies.Academic.Scorebook.ViewOrManage";
        }
    }
}
