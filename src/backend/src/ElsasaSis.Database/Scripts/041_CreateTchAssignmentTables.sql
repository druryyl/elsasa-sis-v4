CREATE TABLE [tch].[teacher_assignments] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [teacher_id] UNIQUEIDENTIFIER NOT NULL,
    [teacher_name] NVARCHAR(150) NOT NULL,
    [academic_year_id] UNIQUEIDENTIFIER NOT NULL,
    [academic_year_name] NVARCHAR(50) NOT NULL,
    [operational_period_id] UNIQUEIDENTIFIER NOT NULL,
    [operational_period_name] NVARCHAR(50) NOT NULL,
    [subject_id] UNIQUEIDENTIFIER NOT NULL,
    [subject_name] NVARCHAR(100) NOT NULL,
    [study_group_id] UNIQUEIDENTIFIER NOT NULL,
    [study_group_name] NVARCHAR(50) NOT NULL,
    [status] VARCHAR(20) NOT NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_teacher_assignments] PRIMARY KEY CLUSTERED ([id]),
    CONSTRAINT [UQ_teacher_assignments_teacher_subject_group_period] UNIQUE ([teacher_id], [subject_id], [study_group_id], [operational_period_id])
);

CREATE NONCLUSTERED INDEX [IX_teacher_assignments_period_group]
    ON [tch].[teacher_assignments] ([operational_period_id], [study_group_id], [status]);

CREATE NONCLUSTERED INDEX [IX_teacher_assignments_period_teacher]
    ON [tch].[teacher_assignments] ([operational_period_id], [teacher_id], [status]);

CREATE NONCLUSTERED INDEX [IX_teacher_assignments_subject_period]
    ON [tch].[teacher_assignments] ([subject_id], [operational_period_id], [status]);
