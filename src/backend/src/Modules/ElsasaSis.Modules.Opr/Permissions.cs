namespace ElsasaSis.Modules.Opr;

public static class Permissions
{
    public static class Operations
    {
        public static class Calendar
        {
            public const string View = "Permissions.Operations.Calendar.View";
            public const string Manage = "Permissions.Operations.Calendar.Manage";
        }

        public static class Rooms
        {
            public const string View = "Permissions.Operations.Rooms.View";
            public const string Manage = "Permissions.Operations.Rooms.Manage";
        }

        public static class Announcements
        {
            public const string View = "Permissions.Operations.Announcements.View";
            public const string Create = "Permissions.Operations.Announcements.Create";
            public const string Publish = "Permissions.Operations.Announcements.Publish";
        }

        public static class Timetables
        {
            public const string View = "Permissions.Operations.Timetables.View";
            public const string Manage = "Permissions.Operations.Timetables.Manage";
            public const string ViewOwn = "Permissions.Operations.Timetables.ViewOwn";
        }
    }
}

public static class Policies
{
    public static class Operations
    {
        public static class Calendar
        {
            public const string ViewOrManage = "Policies.Operations.Calendar.ViewOrManage";
        }

        public static class Rooms
        {
            public const string ViewOrManage = "Policies.Operations.Rooms.ViewOrManage";
        }

        public static class Announcements
        {
            public const string ViewOrManage = "Policies.Operations.Announcements.ViewOrManage";
        }

        public static class Timetables
        {
            public const string ViewOrManageOrOwn = "Policies.Operations.Timetables.ViewOrManageOrOwn";
        }
    }
}
