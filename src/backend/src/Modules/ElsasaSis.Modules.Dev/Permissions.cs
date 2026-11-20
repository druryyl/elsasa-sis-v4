namespace ElsasaSis.Modules.Dev;

public static class Permissions
{
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
}

public static class Policies
{
    public static class Development
    {
        public static class Pastoral
        {
            public const string RecordOrView = "Policies.Development.Pastoral.RecordOrView";
        }
    }
}
