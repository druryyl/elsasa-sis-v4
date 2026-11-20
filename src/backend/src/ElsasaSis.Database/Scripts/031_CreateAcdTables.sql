CREATE TABLE [acd].[subject_stages] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [subject_id] UNIQUEIDENTIFIER NOT NULL,
    [subject_name] NVARCHAR(100) NOT NULL,
    [name] NVARCHAR(100) NOT NULL,
    [sequence_order] INT NOT NULL,
    [description] NVARCHAR(500) NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_subject_stages] PRIMARY KEY CLUSTERED ([id]),
    CONSTRAINT [UQ_subject_stages_subject_name] UNIQUE ([subject_id], [name]),
    CONSTRAINT [UQ_subject_stages_subject_seq] UNIQUE ([subject_id], [sequence_order])
);

CREATE TABLE [acd].[learning_units] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [subject_stage_id] UNIQUEIDENTIFIER NOT NULL,
    [name] NVARCHAR(150) NOT NULL,
    [sequence_order] INT NOT NULL,
    [unit_type] VARCHAR(30) NOT NULL,
    [description] NVARCHAR(500) NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_learning_units] PRIMARY KEY CLUSTERED ([id]),
    CONSTRAINT [UQ_learning_units_stage_name] UNIQUE ([subject_stage_id], [name]),
    CONSTRAINT [UQ_learning_units_stage_seq] UNIQUE ([subject_stage_id], [sequence_order])
);

CREATE TABLE [acd].[subject_stage_grades] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [subject_stage_id] UNIQUEIDENTIFIER NOT NULL,
    [school_level] VARCHAR(20) NOT NULL,
    [grade] VARCHAR(10) NOT NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_subject_stage_grades] PRIMARY KEY CLUSTERED ([id]),
    CONSTRAINT [UQ_subject_stage_grades_stage_level_grade] UNIQUE ([subject_stage_id], [school_level], [grade])
);

CREATE NONCLUSTERED INDEX [IX_subject_stage_grades_level_grade]
    ON [acd].[subject_stage_grades] ([school_level], [grade], [subject_stage_id]);

CREATE TABLE [acd].[academic_participations] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [student_id] UNIQUEIDENTIFIER NOT NULL,
    [student_name] NVARCHAR(150) NOT NULL,
    [student_nis] NVARCHAR(30) NOT NULL,
    [subject_stage_id] UNIQUEIDENTIFIER NOT NULL,
    [subject_stage_name] NVARCHAR(100) NOT NULL,
    [subject_id] UNIQUEIDENTIFIER NOT NULL,
    [subject_name] NVARCHAR(100) NOT NULL,
    [operational_period_id] UNIQUEIDENTIFIER NOT NULL,
    [operational_period_name] NVARCHAR(50) NOT NULL,
    [academic_year_id] UNIQUEIDENTIFIER NOT NULL,
    [academic_year_name] NVARCHAR(50) NOT NULL,
    [study_group_id] UNIQUEIDENTIFIER NULL,
    [study_group_name] NVARCHAR(50) NULL,
    [status] VARCHAR(20) NOT NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_academic_participations] PRIMARY KEY CLUSTERED ([id]),
    CONSTRAINT [UQ_academic_participations_student_stage_period] UNIQUE ([student_id], [subject_stage_id], [operational_period_id])
);

CREATE NONCLUSTERED INDEX [IX_academic_participations_subject_group]
    ON [acd].[academic_participations] ([operational_period_id], [subject_id], [study_group_id], [status]);

CREATE NONCLUSTERED INDEX [IX_academic_participations_stage_group]
    ON [acd].[academic_participations] ([operational_period_id], [subject_stage_id], [study_group_id], [status]);

CREATE NONCLUSTERED INDEX [IX_academic_participations_student_period]
    ON [acd].[academic_participations] ([student_id], [operational_period_id], [status]);

CREATE TABLE [acd].[attendance_records] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [student_id] UNIQUEIDENTIFIER NOT NULL,
    [student_name] NVARCHAR(150) NOT NULL,
    [student_nis] NVARCHAR(30) NOT NULL,
    [subject_id] UNIQUEIDENTIFIER NOT NULL,
    [subject_name] NVARCHAR(100) NOT NULL,
    [study_group_id] UNIQUEIDENTIFIER NULL,
    [study_group_name] NVARCHAR(50) NULL,
    [operational_period_id] UNIQUEIDENTIFIER NOT NULL,
    [operational_period_name] NVARCHAR(50) NOT NULL,
    [attendance_date] DATE NOT NULL,
    [status] VARCHAR(20) NOT NULL,
    [notes] NVARCHAR(500) NULL,
    [recorded_by_teacher_id] UNIQUEIDENTIFIER NOT NULL,
    [recorded_by_teacher_name] NVARCHAR(150) NOT NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_attendance_records] PRIMARY KEY CLUSTERED ([id]),
    CONSTRAINT [UQ_attendance_records_student_subject_date] UNIQUE ([student_id], [subject_id], [attendance_date])
);

CREATE NONCLUSTERED INDEX [IX_attendance_records_subject_group_date]
    ON [acd].[attendance_records] ([subject_id], [study_group_id], [attendance_date]);

CREATE NONCLUSTERED INDEX [IX_attendance_records_student_period]
    ON [acd].[attendance_records] ([student_id], [operational_period_id], [attendance_date]);

CREATE TABLE [acd].[assignments] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [subject_stage_id] UNIQUEIDENTIFIER NOT NULL,
    [subject_stage_name] NVARCHAR(100) NOT NULL,
    [subject_id] UNIQUEIDENTIFIER NOT NULL,
    [subject_name] NVARCHAR(100) NOT NULL,
    [learning_unit_id] UNIQUEIDENTIFIER NULL,
    [learning_unit_name] NVARCHAR(150) NULL,
    [operational_period_id] UNIQUEIDENTIFIER NOT NULL,
    [operational_period_name] NVARCHAR(50) NOT NULL,
    [study_group_id] UNIQUEIDENTIFIER NULL,
    [study_group_name] NVARCHAR(50) NULL,
    [title] NVARCHAR(150) NOT NULL,
    [assignment_type] VARCHAR(20) NOT NULL,
    [max_score] DECIMAL(5,2) NOT NULL,
    [weight] DECIMAL(5,2) NULL,
    [assigned_date] DATE NOT NULL,
    [due_date] DATE NULL,
    [teacher_id] UNIQUEIDENTIFIER NOT NULL,
    [teacher_name] NVARCHAR(150) NOT NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_assignments] PRIMARY KEY CLUSTERED ([id])
);

CREATE NONCLUSTERED INDEX [IX_assignments_stage_group_period]
    ON [acd].[assignments] ([operational_period_id], [subject_stage_id], [study_group_id], [assigned_date]);

CREATE NONCLUSTERED INDEX [IX_assignments_learning_unit]
    ON [acd].[assignments] ([learning_unit_id], [assigned_date])
    WHERE [learning_unit_id] IS NOT NULL;

CREATE TABLE [acd].[assignment_scores] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [assignment_id] UNIQUEIDENTIFIER NOT NULL,
    [student_id] UNIQUEIDENTIFIER NOT NULL,
    [student_name] NVARCHAR(150) NOT NULL,
    [student_nis] NVARCHAR(30) NOT NULL,
    [score] DECIMAL(5,2) NOT NULL,
    [feedback] NVARCHAR(1000) NULL,
    [graded_at] DATETIME2(3) NOT NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_assignment_scores] PRIMARY KEY CLUSTERED ([id]),
    CONSTRAINT [UQ_assignment_scores_assignment_student] UNIQUE ([assignment_id], [student_id])
);

CREATE NONCLUSTERED INDEX [IX_assignment_scores_assignment]
    ON [acd].[assignment_scores] ([assignment_id]);

CREATE NONCLUSTERED INDEX [IX_assignment_scores_student]
    ON [acd].[assignment_scores] ([student_id], [assignment_id]);

CREATE TABLE [acd].[report_cards] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [student_id] UNIQUEIDENTIFIER NOT NULL,
    [student_name] NVARCHAR(150) NOT NULL,
    [student_nis] NVARCHAR(30) NOT NULL,
    [student_nisn] NVARCHAR(20) NULL,
    [academic_year_id] UNIQUEIDENTIFIER NOT NULL,
    [academic_year_name] NVARCHAR(50) NOT NULL,
    [operational_period_id] UNIQUEIDENTIFIER NOT NULL,
    [operational_period_name] NVARCHAR(50) NOT NULL,
    [study_group_id] UNIQUEIDENTIFIER NULL,
    [study_group_name] NVARCHAR(50) NULL,
    [homeroom_teacher_id] UNIQUEIDENTIFIER NULL,
    [homeroom_teacher_name] NVARCHAR(150) NULL,
    [status] VARCHAR(20) NOT NULL,
    [finalized_at] DATETIME2(3) NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_report_cards] PRIMARY KEY CLUSTERED ([id]),
    CONSTRAINT [UQ_report_cards_student_period] UNIQUE ([student_id], [operational_period_id])
);

CREATE NONCLUSTERED INDEX [IX_report_cards_period_group_status]
    ON [acd].[report_cards] ([operational_period_id], [study_group_id], [status]);

CREATE TABLE [acd].[report_card_entries] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [report_card_id] UNIQUEIDENTIFIER NOT NULL,
    [subject_id] UNIQUEIDENTIFIER NOT NULL,
    [subject_name] NVARCHAR(100) NOT NULL,
    [subject_code] NVARCHAR(30) NULL,
    [subject_stage_id] UNIQUEIDENTIFIER NOT NULL,
    [subject_stage_name] NVARCHAR(100) NOT NULL,
    [teacher_id] UNIQUEIDENTIFIER NULL,
    [teacher_name] NVARCHAR(150) NULL,
    [numeric_score] DECIMAL(5,2) NOT NULL,
    [letter_grade] VARCHAR(5) NULL,
    [teacher_notes] NVARCHAR(1000) NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_report_card_entries] PRIMARY KEY CLUSTERED ([id]),
    CONSTRAINT [UQ_report_card_entries_card_stage] UNIQUE ([report_card_id], [subject_stage_id])
);

CREATE NONCLUSTERED INDEX [IX_report_card_entries_report_card]
    ON [acd].[report_card_entries] ([report_card_id]);
