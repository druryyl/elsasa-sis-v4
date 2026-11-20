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

    public static class Development
    {
        public static class Behaviour
        {
            public const string Record = "Permissions.Development.Behaviour.Record";
        }

        public static class Discipline
        {
            public const string Record = "Permissions.Development.Discipline.Record";
            public const string Escalate = "Permissions.Development.Discipline.Escalate";
        }

        public static class Achievements
        {
            public const string Record = "Permissions.Development.Achievements.Record";
        }

        public static class Extracurricular
        {
            public const string Manage = "Permissions.Development.Extracurricular.Manage";
            public const string Log = "Permissions.Development.Extracurricular.Log";
        }

        public static class Dossier
        {
            public const string View = "Permissions.Development.Dossier.View";
        }
    }

    public static class Finance
    {
        public static class FeePolicy
        {
            public const string Manage = "Permissions.Finance.FeePolicy.Manage";
        }

        public static class Discounts
        {
            public const string Manage = "Permissions.Finance.Discounts.Manage";
        }

        public static class Billing
        {
            public const string Generate = "Permissions.Finance.Billing.Generate";
            public const string ManageAdHoc = "Permissions.Finance.Billing.ManageAdHoc";
            public const string Cancel = "Permissions.Finance.Billing.Cancel";
        }

        public static class Payments
        {
            public const string Record = "Permissions.Finance.Payments.Record";
        }

        public static class Deposits
        {
            public const string Allocate = "Permissions.Finance.Deposits.Allocate";
        }

        public static class Statements
        {
            public const string View = "Permissions.Finance.Statements.View";
        }

        public static class Reports
        {
            public const string View = "Permissions.Finance.Reports.View";
        }
    }

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

    public static IReadOnlyList<string> All { get; } =
    [
        Operations.Calendar.View,
        Operations.Calendar.Manage,
        Operations.Rooms.View,
        Operations.Rooms.Manage,
        Operations.Announcements.View,
        Operations.Announcements.Create,
        Operations.Announcements.Publish,
        Students.Admissions.View,
        Students.Admissions.Register,
        Students.Admissions.Decide,
        Students.Admissions.Approve,
        Students.Admissions.Enroll,
        Students.Directory.View,
        Students.Directory.Manage,
        Students.Placements.Manage,
        Students.Roster.ViewOwn,
        Teaching.Directory.Manage,
        Teaching.Capabilities.Manage,
        Teaching.Assignments.View,
        Teaching.Assignments.Manage,
        Teaching.Assignments.Oversight,
        Operations.Timetables.View,
        Operations.Timetables.Manage,
        Operations.Timetables.ViewOwn,
        Academic.Curriculum.View,
        Academic.Curriculum.Manage,
        Academic.Attendance.View,
        Academic.Attendance.Record,
        Academic.Scorebook.View,
        Academic.Scorebook.Manage,
        Academic.ReportCards.View,
        Academic.ReportCards.Generate,
        Academic.ReportCards.ProvideNotes,
        Academic.ReportCards.Remarks,
        Academic.ReportCards.Finalize,
        Academic.ReportCards.Override,
        Development.Behaviour.Record,
        Development.Discipline.Record,
        Development.Discipline.Escalate,
        Development.Achievements.Record,
        Development.Extracurricular.Manage,
        Development.Extracurricular.Log,
        Development.Dossier.View,
        Finance.FeePolicy.Manage,
        Finance.Discounts.Manage,
        Finance.Billing.Generate,
        Finance.Billing.ManageAdHoc,
        Finance.Billing.Cancel,
        Finance.Payments.Record,
        Finance.Deposits.Allocate,
        Finance.Statements.View,
        Finance.Reports.View
    ];
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

    public static class Development
    {
        public static class Pastoral
        {
            public const string RecordOrView = "Policies.Development.Pastoral.RecordOrView";
        }
    }

    public static class Finance
    {
        public static class Cashier
        {
            public const string Manage = "Policies.Finance.Cashier.Manage";
        }

        public static class Reports
        {
            public const string View = "Policies.Finance.Reports.View";
        }
    }

    public static class Students
    {
        public static class Admissions
        {
            public const string DecideOrApprove = "Policies.Students.Admissions.DecideOrApprove";
        }

        public const string DirectoryOrOwnRoster = "Policies.Students.DirectoryOrOwnRoster";
        public const string BillingLookup = "Policies.Students.BillingLookup";
    }

    public static class Teaching
    {
        public static class Assignments
        {
            public const string ViewOrOversight = "Policies.Teaching.Assignments.ViewOrOversight";
        }
    }
}
