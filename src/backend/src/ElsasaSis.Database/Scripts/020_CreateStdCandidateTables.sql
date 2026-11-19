CREATE TABLE [std].[candidates] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [admission_year_id] UNIQUEIDENTIFIER NOT NULL,
    [admission_year_name] NVARCHAR(50) NOT NULL,
    [full_name] NVARCHAR(150) NOT NULL,
    [nickname] NVARCHAR(50) NULL,
    [date_of_birth] DATE NOT NULL,
    [gender] VARCHAR(10) NOT NULL,
    [phone] NVARCHAR(30) NULL,
    [email] NVARCHAR(150) NULL,
    [address] NVARCHAR(500) NULL,
    [target_school_level] VARCHAR(10) NOT NULL,
    [target_grade] VARCHAR(20) NOT NULL,
    [admission_status] VARCHAR(20) NOT NULL,
    [registered_at] DATETIME2(3) NOT NULL,
    [decided_at] DATETIME2(3) NULL,
    [decision_notes] NVARCHAR(500) NULL,
    [enrolled_student_id] UNIQUEIDENTIFIER NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_candidates] PRIMARY KEY CLUSTERED ([id])
);

CREATE NONCLUSTERED INDEX [IX_candidates_admission_lookup]
    ON [std].[candidates] ([admission_year_id], [admission_status], [target_school_level], [target_grade]);

CREATE NONCLUSTERED INDEX [IX_candidates_full_name]
    ON [std].[candidates] ([full_name]);

CREATE TABLE [std].[candidate_guardians] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [candidate_id] UNIQUEIDENTIFIER NOT NULL,
    [full_name] NVARCHAR(150) NOT NULL,
    [relationship_type] VARCHAR(20) NOT NULL,
    [phone] NVARCHAR(30) NULL,
    [email] NVARCHAR(150) NULL,
    [address] NVARCHAR(500) NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_candidate_guardians] PRIMARY KEY CLUSTERED ([id])
);

CREATE NONCLUSTERED INDEX [IX_candidate_guardians_candidate]
    ON [std].[candidate_guardians] ([candidate_id]);
