DECLARE @Now DATETIME2(3) = CAST(SYSUTCDATETIME() AS DATETIME2(3));
DECLARE @Yesterday DATE = DATEADD(DAY, -1, CAST(SYSUTCDATETIME() AS DATE));
DECLARE @AcademicYearId UNIQUEIDENTIFIER = NULL;
DECLARE @AcademicYearName NVARCHAR(50) = NULL;
DECLARE @OperationalPeriodId UNIQUEIDENTIFIER = NULL;
DECLARE @OperationalPeriodName NVARCHAR(50) = NULL;

-- 1. Resolve active Academic Year and active Operational Period
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
END;

IF @AcademicYearId IS NULL
BEGIN
    SET @AcademicYearId = '01A1F11A-600B-700B-8000-00000000000B';
    SET @AcademicYearName = N'2026/2027';

    IF NOT EXISTS (SELECT 1 FROM [opr].[academic_years] WHERE [id] = @AcademicYearId OR [name] = @AcademicYearName)
    BEGIN
        INSERT INTO [opr].[academic_years] ([id], [name], [start_date], [end_date], [status], [created_at], [updated_at])
        VALUES (@AcademicYearId, @AcademicYearName, '2026-07-01', '2027-06-30', 'ACTIVE', @Now, @Now);
    END;
END;

SELECT TOP (1)
    @OperationalPeriodId = [id],
    @OperationalPeriodName = [name]
FROM [opr].[operational_periods]
WHERE [academic_year_id] = @AcademicYearId
  AND [status] = 'ACTIVE'
ORDER BY [start_date] ASC;

IF @OperationalPeriodId IS NULL
BEGIN
    SELECT TOP (1)
        @OperationalPeriodId = [id],
        @OperationalPeriodName = [name]
    FROM [opr].[operational_periods]
    WHERE [academic_year_id] = @AcademicYearId
      AND [name] = N'Semester 1';
END;

IF @OperationalPeriodId IS NULL
BEGIN
    SET @OperationalPeriodId = '01A1F11A-600C-700C-8000-00000000000C';
    SET @OperationalPeriodName = N'Semester 1';

    IF NOT EXISTS (SELECT 1 FROM [opr].[operational_periods] WHERE [id] = @OperationalPeriodId)
    BEGIN
        INSERT INTO [opr].[operational_periods] ([id], [academic_year_id], [name], [start_date], [end_date], [status], [created_at], [updated_at])
        VALUES (@OperationalPeriodId, @AcademicYearId, @OperationalPeriodName, '2026-07-01', '2026-12-20', 'ACTIVE', @Now, @Now);
    END;
END;

-- 2. Resolve teacher Budi Santoso
DECLARE @BudiTeacherId UNIQUEIDENTIFIER = NULL;
DECLARE @BudiTeacherName NVARCHAR(150) = N'Budi Santoso';

SELECT TOP (1)
    @BudiTeacherId = [id],
    @BudiTeacherName = [full_name]
FROM [tch].[teachers]
WHERE [nip] = N'198501012010011001' OR [full_name] = N'Budi Santoso';

IF @BudiTeacherId IS NULL
BEGIN
    SET @BudiTeacherId = '01A1F11A-6010-7010-8000-000000000010';
    SET @BudiTeacherName = N'Budi Santoso';

    IF NOT EXISTS (SELECT 1 FROM [tch].[teachers] WHERE [id] = @BudiTeacherId)
    BEGIN
        INSERT INTO [tch].[teachers] (
            [id], [full_name], [nickname], [nip], [nik], [date_of_birth],
            [gender], [phone], [email], [address], [status], [created_at], [updated_at]
        )
        VALUES (
            @BudiTeacherId,
            @BudiTeacherName,
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

-- 3. Resolve room Classroom 7A
DECLARE @RoomId7A UNIQUEIDENTIFIER = NULL;
DECLARE @RoomName7A NVARCHAR(100) = N'Classroom 7A';

SELECT TOP (1)
    @RoomId7A = [id],
    @RoomName7A = [name]
FROM [opr].[rooms]
WHERE [name] = N'Classroom 7A';

IF @RoomId7A IS NULL
BEGIN
    SET @RoomId7A = '01A1F11A-7001-7001-8000-000000000001';
    SET @RoomName7A = N'Classroom 7A';

    IF NOT EXISTS (SELECT 1 FROM [opr].[rooms] WHERE [id] = @RoomId7A OR [name] = @RoomName7A)
    BEGIN
        INSERT INTO [opr].[rooms] ([id], [name], [room_type], [capacity], [created_at], [updated_at])
        VALUES (@RoomId7A, @RoomName7A, 'CLASSROOM', 32, @Now, @Now);
    END;
END;

-- 4. Resolve study group 7A
DECLARE @StudyGroupId7A UNIQUEIDENTIFIER = NULL;
DECLARE @StudyGroupName7A NVARCHAR(50) = N'7A';

SELECT TOP (1)
    @StudyGroupId7A = [id],
    @StudyGroupName7A = [name]
FROM [std].[study_groups]
WHERE [academic_year_id] = @AcademicYearId
  AND [name] = N'7A';

IF @StudyGroupId7A IS NULL
BEGIN
    SET @StudyGroupId7A = '01A1F11A-6011-7011-8000-000000000011';
    SET @StudyGroupName7A = N'7A';

    IF NOT EXISTS (SELECT 1 FROM [std].[study_groups] WHERE [id] = @StudyGroupId7A)
    BEGIN
        INSERT INTO [std].[study_groups] (
            [id], [academic_year_id], [academic_year_name], [school_level], [grade],
            [name], [homeroom_teacher_id], [homeroom_teacher_name], [created_at], [updated_at]
        )
        VALUES (
            @StudyGroupId7A,
            @AcademicYearId,
            @AcademicYearName,
            'SMP',
            '7',
            @StudyGroupName7A,
            @BudiTeacherId,
            @BudiTeacherName,
            @Now,
            @Now
        );
    END;
END;

-- 5. Resolve subjects MTK and BING
DECLARE @MtkSubjectId UNIQUEIDENTIFIER = NULL;
DECLARE @MtkSubjectName NVARCHAR(100) = N'Matematika';
DECLARE @BingSubjectId UNIQUEIDENTIFIER = NULL;
DECLARE @BingSubjectName NVARCHAR(100) = N'Bahasa Inggris';

SELECT TOP (1)
    @MtkSubjectId = [id],
    @MtkSubjectName = [name]
FROM [acd].[subjects]
WHERE [code] = N'MTK' OR [name] = N'Matematika';

IF @MtkSubjectId IS NULL
BEGIN
    SET @MtkSubjectId = '01A1F11A-6004-7004-8000-000000000004';
    SET @MtkSubjectName = N'Matematika';

    IF NOT EXISTS (SELECT 1 FROM [acd].[subjects] WHERE [id] = @MtkSubjectId)
    BEGIN
        INSERT INTO [acd].[subjects] ([id], [name], [code], [created_at], [updated_at])
        VALUES (@MtkSubjectId, @MtkSubjectName, N'MTK', @Now, @Now);
    END;
END;

SELECT TOP (1)
    @BingSubjectId = [id],
    @BingSubjectName = [name]
FROM [acd].[subjects]
WHERE [code] = N'BING' OR [name] = N'Bahasa Inggris';

IF @BingSubjectId IS NULL
BEGIN
    SET @BingSubjectId = '01A1F11A-6007-7007-8000-000000000007';
    SET @BingSubjectName = N'Bahasa Inggris';

    IF NOT EXISTS (SELECT 1 FROM [acd].[subjects] WHERE [id] = @BingSubjectId)
    BEGIN
        INSERT INTO [acd].[subjects] ([id], [name], [code], [created_at], [updated_at])
        VALUES (@BingSubjectId, @BingSubjectName, N'BING', @Now, @Now);
    END;
END;

-- 6. Ensure teacher assignment for Budi Santoso teaching MTK in 7A
IF NOT EXISTS (
    SELECT 1 FROM [tch].[teacher_assignments]
    WHERE [teacher_id] = @BudiTeacherId
      AND [subject_id] = @MtkSubjectId
      AND [study_group_id] = @StudyGroupId7A
      AND [operational_period_id] = @OperationalPeriodId
)
BEGIN
    INSERT INTO [tch].[teacher_assignments] (
        [id], [teacher_id], [teacher_name], [academic_year_id], [academic_year_name],
        [operational_period_id], [operational_period_name], [subject_id], [subject_name],
        [study_group_id], [study_group_name], [status], [created_at], [updated_at]
    )
    VALUES (
        '01A1F11A-700B-700B-8000-00000000000B',
        @BudiTeacherId,
        @BudiTeacherName,
        @AcademicYearId,
        @AcademicYearName,
        @OperationalPeriodId,
        @OperationalPeriodName,
        @MtkSubjectId,
        @MtkSubjectName,
        @StudyGroupId7A,
        @StudyGroupName7A,
        'ACTIVE',
        @Now,
        @Now
    );
END;

-- 7. Seed subject stages: Mathematics Stage 7 and English Stage 7
DECLARE @MathStageId UNIQUEIDENTIFIER = '01A1F11A-7002-7002-8000-000000000002';
DECLARE @MathStageName NVARCHAR(100) = N'Matematika Tingkat 7';
DECLARE @EnglishStageId UNIQUEIDENTIFIER = '01A1F11A-7003-7003-8000-000000000003';
DECLARE @EnglishStageName NVARCHAR(100) = N'Bahasa Inggris Tingkat 7';

IF NOT EXISTS (SELECT 1 FROM [acd].[subject_stages] WHERE [id] = @MathStageId OR ([subject_id] = @MtkSubjectId AND [name] = @MathStageName))
BEGIN
    INSERT INTO [acd].[subject_stages] ([id], [subject_id], [subject_name], [name], [sequence_order], [description], [created_at], [updated_at])
    VALUES (@MathStageId, @MtkSubjectId, @MtkSubjectName, @MathStageName, 1, N'Kurikulum Matematika SMP Tingkat 7', @Now, @Now);
END
ELSE
BEGIN
    SELECT TOP (1) @MathStageId = [id] FROM [acd].[subject_stages] WHERE [subject_id] = @MtkSubjectId AND [name] = @MathStageName;
END;

IF NOT EXISTS (SELECT 1 FROM [acd].[subject_stages] WHERE [id] = @EnglishStageId OR ([subject_id] = @BingSubjectId AND [name] = @EnglishStageName))
BEGIN
    INSERT INTO [acd].[subject_stages] ([id], [subject_id], [subject_name], [name], [sequence_order], [description], [created_at], [updated_at])
    VALUES (@EnglishStageId, @BingSubjectId, @BingSubjectName, @EnglishStageName, 1, N'Kurikulum Bahasa Inggris SMP Tingkat 7', @Now, @Now);
END
ELSE
BEGIN
    SELECT TOP (1) @EnglishStageId = [id] FROM [acd].[subject_stages] WHERE [subject_id] = @BingSubjectId AND [name] = @EnglishStageName;
END;

-- 8. Seed learning units
DECLARE @MathLu1Id UNIQUEIDENTIFIER = '01A1F11A-7004-7004-8000-000000000004';
DECLARE @MathLu1Name NVARCHAR(150) = N'Bilangan & Aljabar';
DECLARE @MathLu2Id UNIQUEIDENTIFIER = '01A1F11A-7005-7005-8000-000000000005';
DECLARE @MathLu2Name NVARCHAR(150) = N'Geometri & Pengukuran';
DECLARE @EnglishLu1Id UNIQUEIDENTIFIER = '01A1F11A-7006-7006-8000-000000000006';
DECLARE @EnglishLu1Name NVARCHAR(150) = N'Greetings & Introductions';

IF NOT EXISTS (SELECT 1 FROM [acd].[learning_units] WHERE [id] = @MathLu1Id OR ([subject_stage_id] = @MathStageId AND [name] = @MathLu1Name))
BEGIN
    INSERT INTO [acd].[learning_units] ([id], [subject_stage_id], [name], [sequence_order], [unit_type], [description], [created_at], [updated_at])
    VALUES (@MathLu1Id, @MathStageId, @MathLu1Name, 1, 'MODULE', N'Operasi bilangan bulat, pecahan, dan pengantar bentuk aljabar', @Now, @Now);
END
ELSE
BEGIN
    SELECT TOP (1) @MathLu1Id = [id] FROM [acd].[learning_units] WHERE [subject_stage_id] = @MathStageId AND [name] = @MathLu1Name;
END;

IF NOT EXISTS (SELECT 1 FROM [acd].[learning_units] WHERE [id] = @MathLu2Id OR ([subject_stage_id] = @MathStageId AND [name] = @MathLu2Name))
BEGIN
    INSERT INTO [acd].[learning_units] ([id], [subject_stage_id], [name], [sequence_order], [unit_type], [description], [created_at], [updated_at])
    VALUES (@MathLu2Id, @MathStageId, @MathLu2Name, 2, 'MODULE', N'Garis, sudut, bangun datar segiempat dan segitiga', @Now, @Now);
END
ELSE
BEGIN
    SELECT TOP (1) @MathLu2Id = [id] FROM [acd].[learning_units] WHERE [subject_stage_id] = @MathStageId AND [name] = @MathLu2Name;
END;

IF NOT EXISTS (SELECT 1 FROM [acd].[learning_units] WHERE [id] = @EnglishLu1Id OR ([subject_stage_id] = @EnglishStageId AND [name] = @EnglishLu1Name))
BEGIN
    INSERT INTO [acd].[learning_units] ([id], [subject_stage_id], [name], [sequence_order], [unit_type], [description], [created_at], [updated_at])
    VALUES (@EnglishLu1Id, @EnglishStageId, @EnglishLu1Name, 1, 'MODULE', N'Basic interpersonal communication and introductions', @Now, @Now);
END
ELSE
BEGIN
    SELECT TOP (1) @EnglishLu1Id = [id] FROM [acd].[learning_units] WHERE [subject_stage_id] = @EnglishStageId AND [name] = @EnglishLu1Name;
END;

-- 9. Seed grade mappings to (SMP, 7)
IF NOT EXISTS (SELECT 1 FROM [acd].[subject_stage_grades] WHERE [subject_stage_id] = @MathStageId AND [school_level] = 'SMP' AND [grade] = '7')
BEGIN
    INSERT INTO [acd].[subject_stage_grades] ([id], [subject_stage_id], [school_level], [grade], [created_at], [updated_at])
    VALUES ('01A1F11A-7007-7007-8000-000000000007', @MathStageId, 'SMP', '7', @Now, @Now);
END;

IF NOT EXISTS (SELECT 1 FROM [acd].[subject_stage_grades] WHERE [subject_stage_id] = @EnglishStageId AND [school_level] = 'SMP' AND [grade] = '7')
BEGIN
    INSERT INTO [acd].[subject_stage_grades] ([id], [subject_stage_id], [school_level], [grade], [created_at], [updated_at])
    VALUES ('01A1F11A-7008-7008-8000-000000000008', @EnglishStageId, 'SMP', '7', @Now, @Now);
END;

-- 10. Seed study group 7A weekly timetable slots (Monday Period 1-2 MTK with Budi Santoso in Classroom 7A)
IF NOT EXISTS (
    SELECT 1 FROM [opr].[study_group_schedules]
    WHERE [operational_period_id] = @OperationalPeriodId
      AND [study_group_id] = @StudyGroupId7A
      AND [schedule_type] = 'REGULAR'
      AND [day_of_week] = 1
      AND [period_slot] = 1
)
BEGIN
    INSERT INTO [opr].[study_group_schedules] (
        [id], [operational_period_id], [study_group_id], [study_group_name],
        [schedule_type], [day_of_week], [period_slot], [start_time], [end_time],
        [subject_id], [subject_name], [teacher_id], [teacher_name],
        [room_id], [room_name], [created_at], [updated_at]
    )
    VALUES (
        '01A1F11A-7009-7009-8000-000000000009',
        @OperationalPeriodId,
        @StudyGroupId7A,
        @StudyGroupName7A,
        'REGULAR',
        1,
        1,
        '07:30:00',
        '08:15:00',
        @MtkSubjectId,
        @MtkSubjectName,
        @BudiTeacherId,
        @BudiTeacherName,
        @RoomId7A,
        @RoomName7A,
        @Now,
        @Now
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM [opr].[study_group_schedules]
    WHERE [operational_period_id] = @OperationalPeriodId
      AND [study_group_id] = @StudyGroupId7A
      AND [schedule_type] = 'REGULAR'
      AND [day_of_week] = 1
      AND [period_slot] = 2
)
BEGIN
    INSERT INTO [opr].[study_group_schedules] (
        [id], [operational_period_id], [study_group_id], [study_group_name],
        [schedule_type], [day_of_week], [period_slot], [start_time], [end_time],
        [subject_id], [subject_name], [teacher_id], [teacher_name],
        [room_id], [room_name], [created_at], [updated_at]
    )
    VALUES (
        '01A1F11A-700A-700A-8000-00000000000A',
        @OperationalPeriodId,
        @StudyGroupId7A,
        @StudyGroupName7A,
        'REGULAR',
        1,
        2,
        '08:15:00',
        '09:00:00',
        @MtkSubjectId,
        @MtkSubjectName,
        @BudiTeacherId,
        @BudiTeacherName,
        @RoomId7A,
        @RoomName7A,
        @Now,
        @Now
    );
END;

-- 11. Seed placed students in 7A if none exist
DECLARE @Student1Id UNIQUEIDENTIFIER = '01A1F11A-7010-7010-8000-000000000010';
DECLARE @Student1Name NVARCHAR(150) = N'Ahmad Dahlan';
DECLARE @Student1Nis NVARCHAR(30) = N'202607001';
DECLARE @Student2Id UNIQUEIDENTIFIER = '01A1F11A-7011-7011-8000-000000000011';
DECLARE @Student2Name NVARCHAR(150) = N'Siti Nurhaliza';
DECLARE @Student2Nis NVARCHAR(30) = N'202607002';

IF NOT EXISTS (SELECT 1 FROM [std].[students] WHERE [id] = @Student1Id OR [nis] = @Student1Nis)
BEGIN
    INSERT INTO [std].[students] (
        [id], [candidate_id], [nis], [nisn], [full_name], [nickname],
        [date_of_birth], [gender], [phone], [email], [address],
        [enrollment_date], [status], [created_at], [updated_at]
    )
    VALUES (
        @Student1Id,
        NULL,
        @Student1Nis,
        N'0081234561',
        @Student1Name,
        N'Ahmad',
        '2014-04-12',
        'MALE',
        N'081122334401',
        N'ahmad.dahlan@example.com',
        N'Jl. Merdeka No. 10',
        '2026-07-01',
        'ACTIVE',
        @Now,
        @Now
    );
END
ELSE
BEGIN
    SELECT TOP (1) @Student1Id = [id], @Student1Name = [full_name], @Student1Nis = [nis]
    FROM [std].[students]
    WHERE [id] = @Student1Id OR [nis] = @Student1Nis;
END;

IF NOT EXISTS (SELECT 1 FROM [std].[students] WHERE [id] = @Student2Id OR [nis] = @Student2Nis)
BEGIN
    INSERT INTO [std].[students] (
        [id], [candidate_id], [nis], [nisn], [full_name], [nickname],
        [date_of_birth], [gender], [phone], [email], [address],
        [enrollment_date], [status], [created_at], [updated_at]
    )
    VALUES (
        @Student2Id,
        NULL,
        @Student2Nis,
        N'0081234562',
        @Student2Name,
        N'Siti',
        '2014-08-25',
        'FEMALE',
        N'081122334402',
        N'siti.nurhaliza@example.com',
        N'Jl. Sudirman No. 15',
        '2026-07-01',
        'ACTIVE',
        @Now,
        @Now
    );
END
ELSE
BEGIN
    SELECT TOP (1) @Student2Id = [id], @Student2Name = [full_name], @Student2Nis = [nis]
    FROM [std].[students]
    WHERE [id] = @Student2Id OR [nis] = @Student2Nis;
END;

-- Ensure student placements in 7A
IF NOT EXISTS (SELECT 1 FROM [std].[student_placements] WHERE [student_id] = @Student1Id AND [academic_year_id] = @AcademicYearId)
BEGIN
    INSERT INTO [std].[student_placements] (
        [id], [student_id], [academic_year_id], [academic_year_name],
        [school_level], [grade], [study_group_id], [study_group_name],
        [created_at], [updated_at]
    )
    VALUES (
        '01A1F11A-7012-7012-8000-000000000012',
        @Student1Id,
        @AcademicYearId,
        @AcademicYearName,
        'SMP',
        '7',
        @StudyGroupId7A,
        @StudyGroupName7A,
        @Now,
        @Now
    );
END;

IF NOT EXISTS (SELECT 1 FROM [std].[student_placements] WHERE [student_id] = @Student2Id AND [academic_year_id] = @AcademicYearId)
BEGIN
    INSERT INTO [std].[student_placements] (
        [id], [student_id], [academic_year_id], [academic_year_name],
        [school_level], [grade], [study_group_id], [study_group_name],
        [created_at], [updated_at]
    )
    VALUES (
        '01A1F11A-7013-7013-8000-000000000013',
        @Student2Id,
        @AcademicYearId,
        @AcademicYearName,
        'SMP',
        '7',
        @StudyGroupId7A,
        @StudyGroupName7A,
        @Now,
        @Now
    );
END;

-- 12. Batch enroll placed students of study group 7A into Mathematics Stage 7 and English Stage 7
-- Student 1 participations
IF NOT EXISTS (
    SELECT 1 FROM [acd].[academic_participations]
    WHERE [student_id] = @Student1Id
      AND [subject_stage_id] = @MathStageId
      AND [operational_period_id] = @OperationalPeriodId
)
BEGIN
    INSERT INTO [acd].[academic_participations] (
        [id], [student_id], [student_name], [student_nis],
        [subject_stage_id], [subject_stage_name], [subject_id], [subject_name],
        [operational_period_id], [operational_period_name],
        [academic_year_id], [academic_year_name],
        [study_group_id], [study_group_name],
        [status], [created_at], [updated_at]
    )
    VALUES (
        '01A1F11A-7020-7020-8000-000000000020',
        @Student1Id, @Student1Name, @Student1Nis,
        @MathStageId, @MathStageName, @MtkSubjectId, @MtkSubjectName,
        @OperationalPeriodId, @OperationalPeriodName,
        @AcademicYearId, @AcademicYearName,
        @StudyGroupId7A, @StudyGroupName7A,
        'ACTIVE', @Now, @Now
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM [acd].[academic_participations]
    WHERE [student_id] = @Student1Id
      AND [subject_stage_id] = @EnglishStageId
      AND [operational_period_id] = @OperationalPeriodId
)
BEGIN
    INSERT INTO [acd].[academic_participations] (
        [id], [student_id], [student_name], [student_nis],
        [subject_stage_id], [subject_stage_name], [subject_id], [subject_name],
        [operational_period_id], [operational_period_name],
        [academic_year_id], [academic_year_name],
        [study_group_id], [study_group_name],
        [status], [created_at], [updated_at]
    )
    VALUES (
        '01A1F11A-7021-7021-8000-000000000021',
        @Student1Id, @Student1Name, @Student1Nis,
        @EnglishStageId, @EnglishStageName, @BingSubjectId, @BingSubjectName,
        @OperationalPeriodId, @OperationalPeriodName,
        @AcademicYearId, @AcademicYearName,
        @StudyGroupId7A, @StudyGroupName7A,
        'ACTIVE', @Now, @Now
    );
END;

-- Student 2 participations
IF NOT EXISTS (
    SELECT 1 FROM [acd].[academic_participations]
    WHERE [student_id] = @Student2Id
      AND [subject_stage_id] = @MathStageId
      AND [operational_period_id] = @OperationalPeriodId
)
BEGIN
    INSERT INTO [acd].[academic_participations] (
        [id], [student_id], [student_name], [student_nis],
        [subject_stage_id], [subject_stage_name], [subject_id], [subject_name],
        [operational_period_id], [operational_period_name],
        [academic_year_id], [academic_year_name],
        [study_group_id], [study_group_name],
        [status], [created_at], [updated_at]
    )
    VALUES (
        '01A1F11A-7022-7022-8000-000000000022',
        @Student2Id, @Student2Name, @Student2Nis,
        @MathStageId, @MathStageName, @MtkSubjectId, @MtkSubjectName,
        @OperationalPeriodId, @OperationalPeriodName,
        @AcademicYearId, @AcademicYearName,
        @StudyGroupId7A, @StudyGroupName7A,
        'ACTIVE', @Now, @Now
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM [acd].[academic_participations]
    WHERE [student_id] = @Student2Id
      AND [subject_stage_id] = @EnglishStageId
      AND [operational_period_id] = @OperationalPeriodId
)
BEGIN
    INSERT INTO [acd].[academic_participations] (
        [id], [student_id], [student_name], [student_nis],
        [subject_stage_id], [subject_stage_name], [subject_id], [subject_name],
        [operational_period_id], [operational_period_name],
        [academic_year_id], [academic_year_name],
        [study_group_id], [study_group_name],
        [status], [created_at], [updated_at]
    )
    VALUES (
        '01A1F11A-7023-7023-8000-000000000023',
        @Student2Id, @Student2Name, @Student2Nis,
        @EnglishStageId, @EnglishStageName, @BingSubjectId, @BingSubjectName,
        @OperationalPeriodId, @OperationalPeriodName,
        @AcademicYearId, @AcademicYearName,
        @StudyGroupId7A, @StudyGroupName7A,
        'ACTIVE', @Now, @Now
    );
END;

-- 13. Seed sample attendance records for 7A for yesterday's date
IF NOT EXISTS (
    SELECT 1 FROM [acd].[attendance_records]
    WHERE [student_id] = @Student1Id
      AND [subject_id] = @MtkSubjectId
      AND [attendance_date] = @Yesterday
)
BEGIN
    INSERT INTO [acd].[attendance_records] (
        [id], [student_id], [student_name], [student_nis],
        [subject_id], [subject_name], [study_group_id], [study_group_name],
        [operational_period_id], [operational_period_name],
        [attendance_date], [status], [notes],
        [recorded_by_teacher_id], [recorded_by_teacher_name],
        [created_at], [updated_at]
    )
    VALUES (
        '01A1F11A-7030-7030-8000-000000000030',
        @Student1Id, @Student1Name, @Student1Nis,
        @MtkSubjectId, @MtkSubjectName, @StudyGroupId7A, @StudyGroupName7A,
        @OperationalPeriodId, @OperationalPeriodName,
        @Yesterday, 'PRESENT', NULL,
        @BudiTeacherId, @BudiTeacherName,
        @Now, @Now
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM [acd].[attendance_records]
    WHERE [student_id] = @Student2Id
      AND [subject_id] = @MtkSubjectId
      AND [attendance_date] = @Yesterday
)
BEGIN
    INSERT INTO [acd].[attendance_records] (
        [id], [student_id], [student_name], [student_nis],
        [subject_id], [subject_name], [study_group_id], [study_group_name],
        [operational_period_id], [operational_period_name],
        [attendance_date], [status], [notes],
        [recorded_by_teacher_id], [recorded_by_teacher_name],
        [created_at], [updated_at]
    )
    VALUES (
        '01A1F11A-7031-7031-8000-000000000031',
        @Student2Id, @Student2Name, @Student2Nis,
        @MtkSubjectId, @MtkSubjectName, @StudyGroupId7A, @StudyGroupName7A,
        @OperationalPeriodId, @OperationalPeriodName,
        @Yesterday, 'PRESENT', NULL,
        @BudiTeacherId, @BudiTeacherName,
        @Now, @Now
    );
END;

-- 14. Seed sample formative assignment (Kuis Aljabar 1) and summative assignment (Ulangan Harian 1) with student scores
DECLARE @FormativeAssignmentId UNIQUEIDENTIFIER = '01A1F11A-7040-7040-8000-000000000040';
DECLARE @SummativeAssignmentId UNIQUEIDENTIFIER = '01A1F11A-7050-7050-8000-000000000050';

IF NOT EXISTS (
    SELECT 1 FROM [acd].[assignments]
    WHERE [id] = @FormativeAssignmentId
       OR ([operational_period_id] = @OperationalPeriodId AND [subject_stage_id] = @MathStageId AND [study_group_id] = @StudyGroupId7A AND [title] = N'Kuis Aljabar 1')
)
BEGIN
    INSERT INTO [acd].[assignments] (
        [id], [subject_stage_id], [subject_stage_name], [subject_id], [subject_name],
        [learning_unit_id], [learning_unit_name],
        [operational_period_id], [operational_period_name],
        [study_group_id], [study_group_name],
        [title], [assignment_type], [max_score], [weight],
        [assigned_date], [due_date],
        [teacher_id], [teacher_name],
        [created_at], [updated_at]
    )
    VALUES (
        @FormativeAssignmentId,
        @MathStageId, @MathStageName, @MtkSubjectId, @MtkSubjectName,
        @MathLu1Id, @MathLu1Name,
        @OperationalPeriodId, @OperationalPeriodName,
        @StudyGroupId7A, @StudyGroupName7A,
        N'Kuis Aljabar 1', 'FORMATIVE', 100.00, 10.00,
        DATEADD(DAY, -5, CAST(SYSUTCDATETIME() AS DATE)),
        DATEADD(DAY, -3, CAST(SYSUTCDATETIME() AS DATE)),
        @BudiTeacherId, @BudiTeacherName,
        @Now, @Now
    );
END
ELSE
BEGIN
    SELECT TOP (1) @FormativeAssignmentId = [id]
    FROM [acd].[assignments]
    WHERE [id] = @FormativeAssignmentId
       OR ([operational_period_id] = @OperationalPeriodId AND [subject_stage_id] = @MathStageId AND [study_group_id] = @StudyGroupId7A AND [title] = N'Kuis Aljabar 1');
END;

IF NOT EXISTS (SELECT 1 FROM [acd].[assignment_scores] WHERE [assignment_id] = @FormativeAssignmentId AND [student_id] = @Student1Id)
BEGIN
    INSERT INTO [acd].[assignment_scores] (
        [id], [assignment_id], [student_id], [student_name], [student_nis],
        [score], [feedback], [graded_at], [created_at], [updated_at]
    )
    VALUES (
        '01A1F11A-7041-7041-8000-000000000041',
        @FormativeAssignmentId,
        @Student1Id, @Student1Name, @Student1Nis,
        85.00, N'Bagus, perhatikan tanda negatif pada substitusi.',
        @Now, @Now, @Now
    );
END;

IF NOT EXISTS (SELECT 1 FROM [acd].[assignment_scores] WHERE [assignment_id] = @FormativeAssignmentId AND [student_id] = @Student2Id)
BEGIN
    INSERT INTO [acd].[assignment_scores] (
        [id], [assignment_id], [student_id], [student_name], [student_nis],
        [score], [feedback], [graded_at], [created_at], [updated_at]
    )
    VALUES (
        '01A1F11A-7042-7042-8000-000000000042',
        @FormativeAssignmentId,
        @Student2Id, @Student2Name, @Student2Nis,
        92.50, N'Sangat baik dan teliti.',
        @Now, @Now, @Now
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM [acd].[assignments]
    WHERE [id] = @SummativeAssignmentId
       OR ([operational_period_id] = @OperationalPeriodId AND [subject_stage_id] = @MathStageId AND [study_group_id] = @StudyGroupId7A AND [title] = N'Ulangan Harian 1')
)
BEGIN
    INSERT INTO [acd].[assignments] (
        [id], [subject_stage_id], [subject_stage_name], [subject_id], [subject_name],
        [learning_unit_id], [learning_unit_name],
        [operational_period_id], [operational_period_name],
        [study_group_id], [study_group_name],
        [title], [assignment_type], [max_score], [weight],
        [assigned_date], [due_date],
        [teacher_id], [teacher_name],
        [created_at], [updated_at]
    )
    VALUES (
        @SummativeAssignmentId,
        @MathStageId, @MathStageName, @MtkSubjectId, @MtkSubjectName,
        @MathLu1Id, @MathLu1Name,
        @OperationalPeriodId, @OperationalPeriodName,
        @StudyGroupId7A, @StudyGroupName7A,
        N'Ulangan Harian 1', 'SUMMATIVE', 100.00, 25.00,
        DATEADD(DAY, -2, CAST(SYSUTCDATETIME() AS DATE)),
        DATEADD(DAY, -1, CAST(SYSUTCDATETIME() AS DATE)),
        @BudiTeacherId, @BudiTeacherName,
        @Now, @Now
    );
END
ELSE
BEGIN
    SELECT TOP (1) @SummativeAssignmentId = [id]
    FROM [acd].[assignments]
    WHERE [id] = @SummativeAssignmentId
       OR ([operational_period_id] = @OperationalPeriodId AND [subject_stage_id] = @MathStageId AND [study_group_id] = @StudyGroupId7A AND [title] = N'Ulangan Harian 1');
END;

IF NOT EXISTS (SELECT 1 FROM [acd].[assignment_scores] WHERE [assignment_id] = @SummativeAssignmentId AND [student_id] = @Student1Id)
BEGIN
    INSERT INTO [acd].[assignment_scores] (
        [id], [assignment_id], [student_id], [student_name], [student_nis],
        [score], [feedback], [graded_at], [created_at], [updated_at]
    )
    VALUES (
        '01A1F11A-7051-7051-8000-000000000051',
        @SummativeAssignmentId,
        @Student1Id, @Student1Name, @Student1Nis,
        88.00, N'Pertahankan prestasi.',
        @Now, @Now, @Now
    );
END;

IF NOT EXISTS (SELECT 1 FROM [acd].[assignment_scores] WHERE [assignment_id] = @SummativeAssignmentId AND [student_id] = @Student2Id)
BEGIN
    INSERT INTO [acd].[assignment_scores] (
        [id], [assignment_id], [student_id], [student_name], [student_nis],
        [score], [feedback], [graded_at], [created_at], [updated_at]
    )
    VALUES (
        '01A1F11A-7052-7052-8000-000000000052',
        @SummativeAssignmentId,
        @Student2Id, @Student2Name, @Student2Nis,
        95.00, N'Luar biasa, hasil sempurna di bagian aljabar.',
        @Now, @Now, @Now
    );
END;
