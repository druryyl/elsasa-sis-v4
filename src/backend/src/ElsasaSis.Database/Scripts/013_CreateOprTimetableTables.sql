CREATE TABLE [opr].[study_group_schedules] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [operational_period_id] UNIQUEIDENTIFIER NOT NULL,
    [study_group_id] UNIQUEIDENTIFIER NOT NULL,
    [study_group_name] NVARCHAR(50) NOT NULL,
    [schedule_type] VARCHAR(20) NOT NULL,
    [day_of_week] INT NOT NULL,
    [period_slot] INT NOT NULL,
    [start_time] TIME(0) NOT NULL,
    [end_time] TIME(0) NOT NULL,
    [subject_id] UNIQUEIDENTIFIER NOT NULL,
    [subject_name] NVARCHAR(100) NOT NULL,
    [teacher_id] UNIQUEIDENTIFIER NOT NULL,
    [teacher_name] NVARCHAR(150) NOT NULL,
    [room_id] UNIQUEIDENTIFIER NULL,
    [room_name] NVARCHAR(100) NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_study_group_schedules] PRIMARY KEY CLUSTERED ([id]),
    CONSTRAINT [UQ_study_group_schedules_slot] UNIQUE ([operational_period_id], [study_group_id], [schedule_type], [day_of_week], [period_slot])
);

CREATE NONCLUSTERED INDEX [IX_study_group_schedules_period_group]
    ON [opr].[study_group_schedules] ([operational_period_id], [study_group_id], [schedule_type], [day_of_week]);

CREATE NONCLUSTERED INDEX [IX_study_group_schedules_period_teacher]
    ON [opr].[study_group_schedules] ([operational_period_id], [teacher_id], [schedule_type], [day_of_week]);

CREATE NONCLUSTERED INDEX [IX_study_group_schedules_period_room]
    ON [opr].[study_group_schedules] ([operational_period_id], [room_id], [schedule_type], [day_of_week])
    WHERE [room_id] IS NOT NULL;

CREATE TABLE [opr].[study_group_timetables] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [operational_period_id] UNIQUEIDENTIFIER NOT NULL,
    [study_group_id] UNIQUEIDENTIFIER NOT NULL,
    [study_group_name] NVARCHAR(50) NOT NULL,
    [schedule_type] VARCHAR(20) NOT NULL,
    [start_date] DATE NOT NULL,
    [end_date] DATE NOT NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_study_group_timetables] PRIMARY KEY CLUSTERED ([id]),
    CONSTRAINT [UQ_study_group_timetables_period_group_date] UNIQUE ([operational_period_id], [study_group_id], [start_date])
);

CREATE NONCLUSTERED INDEX [IX_study_group_timetables_period_date]
    ON [opr].[study_group_timetables] ([operational_period_id], [start_date], [end_date]);
