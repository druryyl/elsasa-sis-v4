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

        public static class ReportCards
        {
            public const string View = "Permissions.Academic.ReportCards.View";
            public const string Generate = "Permissions.Academic.ReportCards.Generate";
            public const string ProvideNotes = "Permissions.Academic.ReportCards.ProvideNotes";
            public const string Remarks = "Permissions.Academic.ReportCards.Remarks";
            public const string Finalize = "Permissions.Academic.ReportCards.Finalize";
            public const string Override = "Permissions.Academic.ReportCards.Override";
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

        public static class ReportCards
        {
            public const string ViewOrManage = "Policies.Academic.ReportCards.ViewOrManage";
        }
    }
}
