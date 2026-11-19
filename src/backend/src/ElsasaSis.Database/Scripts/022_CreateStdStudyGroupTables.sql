CREATE TABLE [std].[study_groups] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [academic_year_id] UNIQUEIDENTIFIER NOT NULL,
    [academic_year_name] NVARCHAR(50) NOT NULL,
    [school_level] VARCHAR(10) NOT NULL,
    [grade] VARCHAR(20) NOT NULL,
    [name] NVARCHAR(50) NOT NULL,
    [homeroom_teacher_id] UNIQUEIDENTIFIER NULL,
    [homeroom_teacher_name] NVARCHAR(150) NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_study_groups] PRIMARY KEY CLUSTERED ([id]),
    CONSTRAINT [UQ_study_groups_year_name] UNIQUE ([academic_year_id], [name])
);

CREATE NONCLUSTERED INDEX [IX_study_groups_year_level_grade]
    ON [std].[study_groups] ([academic_year_id], [school_level], [grade]);

CREATE TABLE [std].[student_placements] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [student_id] UNIQUEIDENTIFIER NOT NULL,
    [academic_year_id] UNIQUEIDENTIFIER NOT NULL,
    [academic_year_name] NVARCHAR(50) NOT NULL,
    [school_level] VARCHAR(10) NOT NULL,
    [grade] VARCHAR(20) NOT NULL,
    [study_group_id] UNIQUEIDENTIFIER NULL,
    [study_group_name] NVARCHAR(50) NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_student_placements] PRIMARY KEY CLUSTERED ([id]),
    CONSTRAINT [UQ_student_placements_student_year] UNIQUE ([student_id], [academic_year_id])
);

CREATE NONCLUSTERED INDEX [IX_student_placements_study_group]
    ON [std].[student_placements] ([academic_year_id], [study_group_id])
    WHERE [study_group_id] IS NOT NULL;

CREATE NONCLUSTERED INDEX [IX_student_placements_level_grade]
    ON [std].[student_placements] ([academic_year_id], [school_level], [grade]);
