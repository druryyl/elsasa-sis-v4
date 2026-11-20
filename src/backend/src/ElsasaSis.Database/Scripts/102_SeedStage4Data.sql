DECLARE @Now DATETIME2(3) = CAST(SYSUTCDATETIME() AS DATETIME2(3));

-- =====================================================================
-- Deterministic Student IDs
-- =====================================================================
DECLARE @StudentAhmadId UNIQUEIDENTIFIER = (
    SELECT TOP (1) [id]
    FROM [std].[students]
    WHERE [id] = '01A1F11A-7010-7010-8000-000000000010' OR [nis] = N'202607001' OR [full_name] = N'Ahmad Dahlan'
);

IF @StudentAhmadId IS NULL
BEGIN
    SET @StudentAhmadId = '01A1F11A-7010-7010-8000-000000000010';
END;

DECLARE @StudentSitiId UNIQUEIDENTIFIER = (
    SELECT TOP (1) [id]
    FROM [std].[students]
    WHERE [id] = '01A1F11A-7011-7011-8000-000000000011' OR [nis] = N'202607002' OR [full_name] = N'Siti Nurhaliza'
);

IF @StudentSitiId IS NULL
BEGIN
    SET @StudentSitiId = '01A1F11A-7011-7011-8000-000000000011';
END;

-- =====================================================================
-- 1. Seed Parent User: parent.hendra (Password: Parent123!)
-- =====================================================================
DECLARE @ParentUserId UNIQUEIDENTIFIER = (
    SELECT TOP (1) [id]
    FROM [auth].[users]
    WHERE [username] = 'parent.hendra'
);
DECLARE @ParentDisplayName NVARCHAR(100) = N'Hendra Dahlan';
DECLARE @ParentEmail VARCHAR(100) = 'hendra.dahlan@elsasa.sch.id';
DECLARE @ParentPasswordHash VARCHAR(255) = 'AQAAAAIAAYagAAAAEAL96dB1O7ggEUvv/VTL5scqG8XgjRHCUKctuRz2B134vc0dH2d9aZpt00BleSex8g==';

IF @ParentUserId IS NULL
BEGIN
    SET @ParentUserId = '01A1F11A-9001-9001-8000-000000000001';

    IF NOT EXISTS (SELECT 1 FROM [auth].[users] WHERE [id] = @ParentUserId OR [username] = 'parent.hendra')
    BEGIN
        INSERT INTO [auth].[users] (
            [id], [username], [password_hash], [email], [display_name], [is_active], [created_at], [updated_at]
        )
        VALUES (
            @ParentUserId,
            'parent.hendra',
            @ParentPasswordHash,
            @ParentEmail,
            @ParentDisplayName,
            1,
            @Now,
            @Now
        );
    END;
END
ELSE
BEGIN
    UPDATE [auth].[users]
       SET [password_hash] = @ParentPasswordHash,
           [email] = @ParentEmail,
           [display_name] = @ParentDisplayName,
           [is_active] = 1,
           [updated_at] = @Now
     WHERE [id] = @ParentUserId;
END;

IF NOT EXISTS (SELECT 1 FROM [auth].[user_roles] WHERE [user_id] = @ParentUserId AND [role_code] = 'PARENT')
BEGIN
    INSERT INTO [auth].[user_roles] ([user_id], [role_code], [created_at], [updated_at])
    VALUES (@ParentUserId, 'PARENT', @Now, @Now);
END;

-- Linkage: parent.hendra -> Ahmad Dahlan & Siti Nurhaliza
IF NOT EXISTS (SELECT 1 FROM [auth].[user_students] WHERE [user_id] = @ParentUserId AND [student_id] = @StudentAhmadId)
BEGIN
    INSERT INTO [auth].[user_students] ([user_id], [student_id], [created_at])
    VALUES (@ParentUserId, @StudentAhmadId, @Now);
END;

IF NOT EXISTS (SELECT 1 FROM [auth].[user_students] WHERE [user_id] = @ParentUserId AND [student_id] = @StudentSitiId)
BEGIN
    INSERT INTO [auth].[user_students] ([user_id], [student_id], [created_at])
    VALUES (@ParentUserId, @StudentSitiId, @Now);
END;

-- =====================================================================
-- 2. Seed Student User 1: student.ahmad (Password: Student123!)
-- =====================================================================
DECLARE @StudentAhmadUserId UNIQUEIDENTIFIER = (
    SELECT TOP (1) [id]
    FROM [auth].[users]
    WHERE [username] = 'student.ahmad'
);
DECLARE @StudentAhmadDisplayName NVARCHAR(100) = N'Ahmad Dahlan';
DECLARE @StudentAhmadEmail VARCHAR(100) = 'ahmad.dahlan@elsasa.sch.id';
DECLARE @StudentPasswordHash VARCHAR(255) = 'AQAAAAIAAYagAAAAEDWti//fsslZxGX/+lg1Rj7VbYBT9fJJ7wTcb9kMvzWYtvaccE3XhogyW1Zb5QUjUw==';

IF @StudentAhmadUserId IS NULL
BEGIN
    SET @StudentAhmadUserId = '01A1F11A-9002-9002-8000-000000000002';

    IF NOT EXISTS (SELECT 1 FROM [auth].[users] WHERE [id] = @StudentAhmadUserId OR [username] = 'student.ahmad')
    BEGIN
        INSERT INTO [auth].[users] (
            [id], [username], [password_hash], [email], [display_name], [is_active], [created_at], [updated_at]
        )
        VALUES (
            @StudentAhmadUserId,
            'student.ahmad',
            @StudentPasswordHash,
            @StudentAhmadEmail,
            @StudentAhmadDisplayName,
            1,
            @Now,
            @Now
        );
    END;
END
ELSE
BEGIN
    UPDATE [auth].[users]
       SET [password_hash] = @StudentPasswordHash,
           [email] = @StudentAhmadEmail,
           [display_name] = @StudentAhmadDisplayName,
           [is_active] = 1,
           [updated_at] = @Now
     WHERE [id] = @StudentAhmadUserId;
END;

IF NOT EXISTS (SELECT 1 FROM [auth].[user_roles] WHERE [user_id] = @StudentAhmadUserId AND [role_code] = 'STUDENT')
BEGIN
    INSERT INTO [auth].[user_roles] ([user_id], [role_code], [created_at], [updated_at])
    VALUES (@StudentAhmadUserId, 'STUDENT', @Now, @Now);
END;

-- Link actor: student.ahmad -> Ahmad Dahlan
IF NOT EXISTS (SELECT 1 FROM [auth].[user_actors] WHERE [user_id] = @StudentAhmadUserId)
BEGIN
    INSERT INTO [auth].[user_actors] ([user_id], [teacher_id], [student_id], [guardian_id], [created_at], [updated_at])
    VALUES (@StudentAhmadUserId, NULL, @StudentAhmadId, NULL, @Now, @Now);
END
ELSE
BEGIN
    UPDATE [auth].[user_actors]
       SET [student_id] = @StudentAhmadId,
           [updated_at] = @Now
     WHERE [user_id] = @StudentAhmadUserId;
END;

-- =====================================================================
-- 3. Seed Student User 2: student.siti (Password: Student123!)
-- =====================================================================
DECLARE @StudentSitiUserId UNIQUEIDENTIFIER = (
    SELECT TOP (1) [id]
    FROM [auth].[users]
    WHERE [username] = 'student.siti'
);
DECLARE @StudentSitiDisplayName NVARCHAR(100) = N'Siti Nurhaliza';
DECLARE @StudentSitiEmail VARCHAR(100) = 'siti.nurhaliza@elsasa.sch.id';

IF @StudentSitiUserId IS NULL
BEGIN
    SET @StudentSitiUserId = '01A1F11A-9003-9003-8000-000000000003';

    IF NOT EXISTS (SELECT 1 FROM [auth].[users] WHERE [id] = @StudentSitiUserId OR [username] = 'student.siti')
    BEGIN
        INSERT INTO [auth].[users] (
            [id], [username], [password_hash], [email], [display_name], [is_active], [created_at], [updated_at]
        )
        VALUES (
            @StudentSitiUserId,
            'student.siti',
            @StudentPasswordHash,
            @StudentSitiEmail,
            @StudentSitiDisplayName,
            1,
            @Now,
            @Now
        );
    END;
END
ELSE
BEGIN
    UPDATE [auth].[users]
       SET [password_hash] = @StudentPasswordHash,
           [email] = @StudentSitiEmail,
           [display_name] = @StudentSitiDisplayName,
           [is_active] = 1,
           [updated_at] = @Now
     WHERE [id] = @StudentSitiUserId;
END;

IF NOT EXISTS (SELECT 1 FROM [auth].[user_roles] WHERE [user_id] = @StudentSitiUserId AND [role_code] = 'STUDENT')
BEGIN
    INSERT INTO [auth].[user_roles] ([user_id], [role_code], [created_at], [updated_at])
    VALUES (@StudentSitiUserId, 'STUDENT', @Now, @Now);
END;

-- Link actor: student.siti -> Siti Nurhaliza
IF NOT EXISTS (SELECT 1 FROM [auth].[user_actors] WHERE [user_id] = @StudentSitiUserId)
BEGIN
    INSERT INTO [auth].[user_actors] ([user_id], [teacher_id], [student_id], [guardian_id], [created_at], [updated_at])
    VALUES (@StudentSitiUserId, NULL, @StudentSitiId, NULL, @Now, @Now);
END
ELSE
BEGIN
    UPDATE [auth].[user_actors]
       SET [student_id] = @StudentSitiId,
           [updated_at] = @Now
     WHERE [user_id] = @StudentSitiUserId;
END;
