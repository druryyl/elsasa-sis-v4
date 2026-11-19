DECLARE @Now DATETIME2(3) = CAST(SYSUTCDATETIME() AS DATETIME2(3));
DECLARE @AcademicYearId UNIQUEIDENTIFIER = NULL;
DECLARE @AcademicYearName NVARCHAR(50) = NULL;
DECLARE @PrincipalUserId UNIQUEIDENTIFIER = NULL;
DECLARE @TeacherBudiUserId UNIQUEIDENTIFIER = NULL;
DECLARE @BudiTeacherId UNIQUEIDENTIFIER = NULL;

-- 1. Ensure an active Academic Year exists (default 2026/2027 with Semester 1 ACTIVE & Semester 2 INACTIVE)
SELECT TOP (1)
    @AcademicYearId = [id],
    @AcademicYearName = [name]
FROM [opr].[academic_years]
WHERE [status] = 'ACTIVE'
ORDER BY [created_at] ASC;

IF @AcademicYearId IS NULL
BEGIN
    SELECT TOP (1)
        @AcademicYearId = [id],
        @AcademicYearName = [name]
    FROM [opr].[academic_years]
    WHERE [name] = N'2026/2027';

    IF @AcademicYearId IS NULL
    BEGIN
        SET @AcademicYearId = '01A1F11A-600B-700B-8000-00000000000B';
        SET @AcademicYearName = N'2026/2027';

        IF NOT EXISTS (SELECT 1 FROM [opr].[academic_years] WHERE [id] = @AcademicYearId OR [name] = @AcademicYearName)
        BEGIN
            INSERT INTO [opr].[academic_years] ([id], [name], [start_date], [end_date], [status], [created_at], [updated_at])
            VALUES (@AcademicYearId, @AcademicYearName, '2026-07-01', '2027-06-30', 'ACTIVE', @Now, @Now);
        END;
    END
    ELSE
    BEGIN
        UPDATE [opr].[academic_years]
           SET [status] = 'ACTIVE',
               [updated_at] = @Now
         WHERE [id] = @AcademicYearId;
    END;

    IF NOT EXISTS (SELECT 1 FROM [opr].[operational_periods] WHERE [academic_year_id] = @AcademicYearId)
    BEGIN
        DECLARE @Semester1Status VARCHAR(20) = CASE
            WHEN EXISTS (SELECT 1 FROM [opr].[operational_periods] WHERE [status] = 'ACTIVE')
                THEN 'INACTIVE'
            ELSE 'ACTIVE'
        END;

        IF NOT EXISTS (SELECT 1 FROM [opr].[operational_periods] WHERE [id] = '01A1F11A-600C-700C-8000-00000000000C')
        BEGIN
            INSERT INTO [opr].[operational_periods] ([id], [academic_year_id], [name], [start_date], [end_date], [status], [created_at], [updated_at])
            VALUES ('01A1F11A-600C-700C-8000-00000000000C', @AcademicYearId, N'Semester 1', '2026-07-01', '2026-12-20', @Semester1Status, @Now, @Now);
        END;

        IF NOT EXISTS (SELECT 1 FROM [opr].[operational_periods] WHERE [id] = '01A1F11A-600D-700D-8000-00000000000D')
        BEGIN
            INSERT INTO [opr].[operational_periods] ([id], [academic_year_id], [name], [start_date], [end_date], [status], [created_at], [updated_at])
            VALUES ('01A1F11A-600D-700D-8000-00000000000D', @AcademicYearId, N'Semester 2', '2027-01-05', '2027-06-30', 'INACTIVE', @Now, @Now);
        END;
    END
    ELSE IF NOT EXISTS (SELECT 1 FROM [opr].[operational_periods] WHERE [status] = 'ACTIVE')
    BEGIN
        UPDATE [opr].[operational_periods]
           SET [status] = 'ACTIVE',
               [updated_at] = @Now
         WHERE [id] = (
             SELECT TOP (1) [id]
             FROM [opr].[operational_periods]
             WHERE [academic_year_id] = @AcademicYearId
             ORDER BY [start_date] ASC
         );
    END;
END;

-- 2. Seed principal user (Principal123!) and PRINCIPAL role
SELECT @PrincipalUserId = [id]
FROM [auth].[users]
WHERE [username] = 'principal';

IF @PrincipalUserId IS NULL
BEGIN
    SET @PrincipalUserId = '01A1F11A-600E-700E-8000-00000000000E';

    IF NOT EXISTS (SELECT 1 FROM [auth].[users] WHERE [id] = @PrincipalUserId OR [username] = 'principal')
    BEGIN
        INSERT INTO [auth].[users] ([id], [username], [password_hash], [email], [display_name], [is_active], [created_at], [updated_at])
        VALUES (
            @PrincipalUserId,
            'principal',
            'AQAAAAIAAYagAAAAEET4qMNaWLCVUkBlCtbM+Fwj3lorx4lgiDBW5niAmB2BfMAtQauHAmBGnLvqq/gHpg==',
            NULL,
            N'School Principal',
            1,
            @Now,
            @Now
        );
    END;
END;

IF NOT EXISTS (SELECT 1 FROM [auth].[user_roles] WHERE [user_id] = @PrincipalUserId AND [role_code] = 'PRINCIPAL')
BEGIN
    INSERT INTO [auth].[user_roles] ([user_id], [role_code], [created_at], [updated_at])
    VALUES (@PrincipalUserId, 'PRINCIPAL', @Now, @Now);
END;

-- 3. Seed teacher.budi user (Teacher123!) and TEACHER + HOMEROOM_TEACHER roles
SELECT @TeacherBudiUserId = [id]
FROM [auth].[users]
WHERE [username] = 'teacher.budi';

IF @TeacherBudiUserId IS NULL
BEGIN
    SET @TeacherBudiUserId = '01A1F11A-600F-700F-8000-00000000000F';

    IF NOT EXISTS (SELECT 1 FROM [auth].[users] WHERE [id] = @TeacherBudiUserId OR [username] = 'teacher.budi')
    BEGIN
        INSERT INTO [auth].[users] ([id], [username], [password_hash], [email], [display_name], [is_active], [created_at], [updated_at])
        VALUES (
            @TeacherBudiUserId,
            'teacher.budi',
            'AQAAAAIAAYagAAAAEPSiv0kFmCGkTuZJUXoxLbkwaz3u+V5X+vMDl5/WC5hVkFMG1M4A4KaA2TaBpkeNjg==',
            NULL,
            N'Budi Santoso',
            1,
            @Now,
            @Now
        );
    END;
END;

IF NOT EXISTS (SELECT 1 FROM [auth].[user_roles] WHERE [user_id] = @TeacherBudiUserId AND [role_code] = 'TEACHER')
BEGIN
    INSERT INTO [auth].[user_roles] ([user_id], [role_code], [created_at], [updated_at])
    VALUES (@TeacherBudiUserId, 'TEACHER', @Now, @Now);
END;

IF NOT EXISTS (SELECT 1 FROM [auth].[user_roles] WHERE [user_id] = @TeacherBudiUserId AND [role_code] = 'HOMEROOM_TEACHER')
BEGIN
    INSERT INTO [auth].[user_roles] ([user_id], [role_code], [created_at], [updated_at])
    VALUES (@TeacherBudiUserId, 'HOMEROOM_TEACHER', @Now, @Now);
END;

-- 4. Seed active teacher profile Budi Santoso and link via auth.user_actors
SELECT @BudiTeacherId = [id]
FROM [tch].[teachers]
WHERE [nip] = N'198501012010011001';

IF @BudiTeacherId IS NULL
BEGIN
    SET @BudiTeacherId = '01A1F11A-6010-7010-8000-000000000010';

    IF NOT EXISTS (SELECT 1 FROM [tch].[teachers] WHERE [id] = @BudiTeacherId OR [nip] = N'198501012010011001')
    BEGIN
        INSERT INTO [tch].[teachers] (
            [id], [full_name], [nickname], [nip], [nik], [date_of_birth],
            [gender], [phone], [email], [address], [status], [created_at], [updated_at]
        )
        VALUES (
            @BudiTeacherId,
            N'Budi Santoso',
            N'Budi',
            N'198501012010011001',
            NULL,
            '1985-01-01',
            'MALE',
            N'081234567890',
            NULL,
            NULL,
            'ACTIVE',
            @Now,
            @Now
        );
    END;
END;

IF NOT EXISTS (SELECT 1 FROM [auth].[user_actors] WHERE [user_id] = @TeacherBudiUserId)
BEGIN
    INSERT INTO [auth].[user_actors] ([user_id], [teacher_id], [student_id], [guardian_id], [created_at], [updated_at])
    VALUES (@TeacherBudiUserId, @BudiTeacherId, NULL, NULL, @Now, @Now);
END
ELSE
BEGIN
    UPDATE [auth].[user_actors]
       SET [teacher_id] = @BudiTeacherId,
           [updated_at] = @Now
     WHERE [user_id] = @TeacherBudiUserId
       AND [teacher_id] IS NULL;
END;

-- 5. Seed study groups 7A (with Budi Santoso homeroom) and 7B (unassigned) under active Academic Year
IF NOT EXISTS (SELECT 1 FROM [std].[study_groups] WHERE [academic_year_id] = @AcademicYearId AND [name] = N'7A')
   AND NOT EXISTS (SELECT 1 FROM [std].[study_groups] WHERE [id] = '01A1F11A-6011-7011-8000-000000000011')
BEGIN
    INSERT INTO [std].[study_groups] (
        [id], [academic_year_id], [academic_year_name], [school_level], [grade],
        [name], [homeroom_teacher_id], [homeroom_teacher_name], [created_at], [updated_at]
    )
    VALUES (
        '01A1F11A-6011-7011-8000-000000000011',
        @AcademicYearId,
        @AcademicYearName,
        'SMP',
        '7',
        N'7A',
        @BudiTeacherId,
        N'Budi Santoso',
        @Now,
        @Now
    );
END;

IF NOT EXISTS (SELECT 1 FROM [std].[study_groups] WHERE [academic_year_id] = @AcademicYearId AND [name] = N'7B')
   AND NOT EXISTS (SELECT 1 FROM [std].[study_groups] WHERE [id] = '01A1F11A-6012-7012-8000-000000000012')
BEGIN
    INSERT INTO [std].[study_groups] (
        [id], [academic_year_id], [academic_year_name], [school_level], [grade],
        [name], [homeroom_teacher_id], [homeroom_teacher_name], [created_at], [updated_at]
    )
    VALUES (
        '01A1F11A-6012-7012-8000-000000000012',
        @AcademicYearId,
        @AcademicYearName,
        'SMP',
        '7',
        N'7B',
        NULL,
        NULL,
        @Now,
        @Now
    );
END;
