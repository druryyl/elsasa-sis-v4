CREATE TABLE [dev].[behaviour_notes] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [student_id] UNIQUEIDENTIFIER NOT NULL,
    [student_name] NVARCHAR(150) NOT NULL,
    [student_nis] NVARCHAR(30) NOT NULL,
    [academic_year_id] UNIQUEIDENTIFIER NULL,
    [academic_year_name] NVARCHAR(50) NULL,
    [behaviour_type] VARCHAR(20) NOT NULL,
    [observed_date] DATE NOT NULL,
    [description] NVARCHAR(1000) NOT NULL,
    [recorder_id] UNIQUEIDENTIFIER NOT NULL,
    [recorder_name] NVARCHAR(150) NOT NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_behaviour_notes] PRIMARY KEY CLUSTERED ([id])
);

CREATE NONCLUSTERED INDEX [IX_behaviour_notes_student_date]
    ON [dev].[behaviour_notes] ([student_id], [observed_date]);

CREATE TABLE [dev].[discipline_records] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [student_id] UNIQUEIDENTIFIER NOT NULL,
    [student_name] NVARCHAR(150) NOT NULL,
    [student_nis] NVARCHAR(30) NOT NULL,
    [academic_year_id] UNIQUEIDENTIFIER NULL,
    [academic_year_name] NVARCHAR(50) NULL,
    [incident_date] DATE NOT NULL,
    [violation_type] VARCHAR(50) NOT NULL,
    [description] NVARCHAR(1000) NOT NULL,
    [sanction] VARCHAR(30) NOT NULL,
    [recorder_id] UNIQUEIDENTIFIER NOT NULL,
    [recorder_name] NVARCHAR(150) NOT NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_discipline_records] PRIMARY KEY CLUSTERED ([id])
);

CREATE NONCLUSTERED INDEX [IX_discipline_records_student_date]
    ON [dev].[discipline_records] ([student_id], [incident_date]);

CREATE TABLE [dev].[extracurricular_activities] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [name] NVARCHAR(100) NOT NULL,
    [description] NVARCHAR(500) NULL,
    [supervisor_id] UNIQUEIDENTIFIER NOT NULL,
    [supervisor_name] NVARCHAR(150) NOT NULL,
    [status] VARCHAR(20) NOT NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_extracurricular_activities] PRIMARY KEY CLUSTERED ([id]),
    CONSTRAINT [UQ_extracurricular_activities_name] UNIQUE ([name])
);

CREATE NONCLUSTERED INDEX [IX_extracurricular_activities_supervisor]
    ON [dev].[extracurricular_activities] ([supervisor_id], [status]);

CREATE TABLE [dev].[extracurricular_participations] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [activity_id] UNIQUEIDENTIFIER NOT NULL,
    [activity_name] NVARCHAR(100) NOT NULL,
    [student_id] UNIQUEIDENTIFIER NOT NULL,
    [student_name] NVARCHAR(150) NOT NULL,
    [student_nis] NVARCHAR(30) NOT NULL,
    [academic_year_id] UNIQUEIDENTIFIER NOT NULL,
    [academic_year_name] NVARCHAR(50) NOT NULL,
    [status] VARCHAR(20) NOT NULL,
    [joined_date] DATE NOT NULL,
    [completed_at] DATETIME2(3) NULL,
    [withdrawn_at] DATETIME2(3) NULL,
    [last_session_log] NVARCHAR(1000) NULL,
    [last_session_date] DATE NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_extracurricular_participations] PRIMARY KEY CLUSTERED ([id]),
    CONSTRAINT [UQ_extracurricular_participations_student_activity_year] UNIQUE ([student_id], [activity_id], [academic_year_id])
);

CREATE NONCLUSTERED INDEX [IX_extracurricular_participations_activity_year]
    ON [dev].[extracurricular_participations] ([activity_id], [academic_year_id], [status]);

CREATE NONCLUSTERED INDEX [IX_extracurricular_participations_student_year]
    ON [dev].[extracurricular_participations] ([student_id], [academic_year_id]);

CREATE TABLE [dev].[achievements] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [student_id] UNIQUEIDENTIFIER NOT NULL,
    [student_name] NVARCHAR(150) NOT NULL,
    [student_nis] NVARCHAR(30) NOT NULL,
    [academic_year_id] UNIQUEIDENTIFIER NULL,
    [academic_year_name] NVARCHAR(50) NULL,
    [achievement_type] VARCHAR(30) NOT NULL,
    [event_name] NVARCHAR(200) NOT NULL,
    [recognition_level] VARCHAR(30) NOT NULL,
    [achievement_date] DATE NOT NULL,
    [description] NVARCHAR(500) NULL,
    [recorder_id] UNIQUEIDENTIFIER NOT NULL,
    [recorder_name] NVARCHAR(150) NOT NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_achievements] PRIMARY KEY CLUSTERED ([id])
);

CREATE NONCLUSTERED INDEX [IX_achievements_student_date]
    ON [dev].[achievements] ([student_id], [achievement_date]);
