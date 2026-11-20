namespace ElsasaSis.Modules.Std;

public static class Permissions
{
    public static class Students
    {
        public static class Admissions
        {
            public const string View = "Permissions.Students.Admissions.View";
            public const string Register = "Permissions.Students.Admissions.Register";
            public const string Decide = "Permissions.Students.Admissions.Decide";
            public const string Approve = "Permissions.Students.Admissions.Approve";
            public const string Enroll = "Permissions.Students.Admissions.Enroll";
        }

        public static class Directory
        {
            public const string View = "Permissions.Students.Directory.View";
            public const string Manage = "Permissions.Students.Directory.Manage";
        }

        public static class Placements
        {
            public const string Manage = "Permissions.Students.Placements.Manage";
        }

        public static class Roster
        {
            public const string ViewOwn = "Permissions.Students.Roster.ViewOwn";
        }
    }
}

public static class Policies
{
    public static class Students
    {
        public static class Admissions
        {
            public const string DecideOrApprove = "Policies.Students.Admissions.DecideOrApprove";
        }

        public const string DirectoryOrOwnRoster = "Policies.Students.DirectoryOrOwnRoster";
        public const string BillingLookup = "Policies.Students.BillingLookup";
    }
}
