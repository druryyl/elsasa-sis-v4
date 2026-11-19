CREATE TABLE [tch].[teachers] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [full_name] NVARCHAR(150) NOT NULL,
    [nickname] NVARCHAR(50) NULL,
    [nip] NVARCHAR(30) NULL,
    [nik] NVARCHAR(30) NULL,
    [date_of_birth] DATE NOT NULL,
    [gender] VARCHAR(10) NOT NULL,
    [phone] NVARCHAR(30) NULL,
    [email] NVARCHAR(150) NULL,
    [address] NVARCHAR(500) NULL,
    [status] VARCHAR(20) NOT NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_teachers] PRIMARY KEY CLUSTERED ([id])
);

CREATE UNIQUE NONCLUSTERED INDEX [UQ_teachers_nip]
    ON [tch].[teachers] ([nip])
    WHERE [nip] IS NOT NULL;

CREATE UNIQUE NONCLUSTERED INDEX [UQ_teachers_nik]
    ON [tch].[teachers] ([nik])
    WHERE [nik] IS NOT NULL;

CREATE NONCLUSTERED INDEX [IX_teachers_status_name]
    ON [tch].[teachers] ([status], [full_name]);

CREATE TABLE [tch].[teaching_capabilities] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [teacher_id] UNIQUEIDENTIFIER NOT NULL,
    [subject_id] UNIQUEIDENTIFIER NOT NULL,
    [subject_name] NVARCHAR(100) NOT NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_teaching_capabilities] PRIMARY KEY CLUSTERED ([id]),
    CONSTRAINT [UQ_teaching_capabilities_teacher_subject] UNIQUE ([teacher_id], [subject_id])
);

CREATE NONCLUSTERED INDEX [IX_teaching_capabilities_teacher]
    ON [tch].[teaching_capabilities] ([teacher_id]);
