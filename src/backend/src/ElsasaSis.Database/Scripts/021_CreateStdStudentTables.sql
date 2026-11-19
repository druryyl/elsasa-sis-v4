CREATE TABLE [std].[students] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [candidate_id] UNIQUEIDENTIFIER NULL,
    [nis] NVARCHAR(30) NOT NULL,
    [nisn] NVARCHAR(20) NULL,
    [full_name] NVARCHAR(150) NOT NULL,
    [nickname] NVARCHAR(50) NULL,
    [date_of_birth] DATE NOT NULL,
    [gender] VARCHAR(10) NOT NULL,
    [phone] NVARCHAR(30) NULL,
    [email] NVARCHAR(150) NULL,
    [address] NVARCHAR(500) NULL,
    [enrollment_date] DATE NOT NULL,
    [status] VARCHAR(20) NOT NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_students] PRIMARY KEY CLUSTERED ([id]),
    CONSTRAINT [UQ_students_nis] UNIQUE ([nis])
);

CREATE UNIQUE NONCLUSTERED INDEX [UQ_students_nisn]
    ON [std].[students] ([nisn])
    WHERE [nisn] IS NOT NULL;

CREATE NONCLUSTERED INDEX [IX_students_status_name]
    ON [std].[students] ([status], [full_name]);

CREATE TABLE [std].[student_guardians] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [student_id] UNIQUEIDENTIFIER NOT NULL,
    [full_name] NVARCHAR(150) NOT NULL,
    [relationship_type] VARCHAR(20) NOT NULL,
    [phone] NVARCHAR(30) NULL,
    [email] NVARCHAR(150) NULL,
    [address] NVARCHAR(500) NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_student_guardians] PRIMARY KEY CLUSTERED ([id])
);

CREATE NONCLUSTERED INDEX [IX_student_guardians_student]
    ON [std].[student_guardians] ([student_id]);
