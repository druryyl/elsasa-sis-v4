namespace ElsasaSis.Modules.Fin;

public static class Permissions
{
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
}

public static class Policies
{
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
}
