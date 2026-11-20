IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[auth].[user_students]') AND type = N'U')
BEGIN
    CREATE TABLE [auth].[user_students] (
        [user_id]    UNIQUEIDENTIFIER NOT NULL,
        [student_id] UNIQUEIDENTIFIER NOT NULL,
        [created_at] DATETIME2(3)     NOT NULL,
        CONSTRAINT [PK_auth_user_students] PRIMARY KEY CLUSTERED ([user_id], [student_id]),
        CONSTRAINT [FK_auth_user_students_users] FOREIGN KEY ([user_id]) REFERENCES [auth].[users] ([id])
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[auth].[user_students]') AND name = N'IX_auth_user_students_student_id')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_auth_user_students_student_id]
        ON [auth].[user_students] ([student_id]);
END;
