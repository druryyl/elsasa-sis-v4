DECLARE @Now DATETIME2(3) = CAST(SYSUTCDATETIME() AS DATETIME2(3));
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

-- 2. Resolve Teacher Budi Santoso
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

-- 3. Resolve Study Group 7A
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

-- 4. Resolve Students Ahmad Dahlan & Siti Nurhaliza
DECLARE @Student1Id UNIQUEIDENTIFIER = '01A1F11A-7010-7010-8000-000000000010';
DECLARE @Student1Name NVARCHAR(150) = N'Ahmad Dahlan';
DECLARE @Student1Nis NVARCHAR(30) = N'202607001';
DECLARE @Student2Id UNIQUEIDENTIFIER = '01A1F11A-7011-7011-8000-000000000011';
DECLARE @Student2Name NVARCHAR(150) = N'Siti Nurhaliza';
DECLARE @Student2Nis NVARCHAR(30) = N'202607002';

SELECT TOP (1) @Student1Id = [id], @Student1Name = [full_name], @Student1Nis = [nis]
FROM [std].[students]
WHERE [id] = @Student1Id OR [nis] = @Student1Nis;

SELECT TOP (1) @Student2Id = [id], @Student2Name = [full_name], @Student2Nis = [nis]
FROM [std].[students]
WHERE [id] = @Student2Id OR [nis] = @Student2Nis;

-- 5. Seed Finance Officer user & role
DECLARE @FinanceUserId UNIQUEIDENTIFIER = '01A1F11A-8001-8001-8000-000000000001';
DECLARE @FinanceUserName NVARCHAR(150) = N'Siti Rahma';

SELECT TOP (1) @FinanceUserId = [id], @FinanceUserName = [display_name]
FROM [auth].[users]
WHERE [username] = 'finance.officer';

IF @FinanceUserId IS NULL OR NOT EXISTS (SELECT 1 FROM [auth].[users] WHERE [id] = @FinanceUserId)
BEGIN
    SET @FinanceUserId = '01A1F11A-8001-8001-8000-000000000001';
    SET @FinanceUserName = N'Siti Rahma';

    IF NOT EXISTS (SELECT 1 FROM [auth].[users] WHERE [id] = @FinanceUserId OR [username] = 'finance.officer')
    BEGIN
        INSERT INTO [auth].[users] (
            [id], [username], [password_hash], [email], [display_name], [is_active], [created_at], [updated_at]
        )
        VALUES (
            @FinanceUserId,
            'finance.officer',
            'AQAAAAIAAYagAAAAEPSiv0kFmCGkTuZJUXoxLbkwaz3u+V5X+vMDl5/WC5hVkFMG1M4A4KaA2TaBpkeNjg==',
            N'finance@elsasa.sch.id',
            @FinanceUserName,
            1,
            @Now,
            @Now
        );
    END;
END;

IF NOT EXISTS (SELECT 1 FROM [auth].[user_roles] WHERE [user_id] = @FinanceUserId AND [role_code] = 'FINANCE_OFFICER')
BEGIN
    INSERT INTO [auth].[user_roles] ([user_id], [role_code], [created_at], [updated_at])
    VALUES (@FinanceUserId, 'FINANCE_OFFICER', @Now, @Now);
END;

-- 6. Seed Fee Structures (fin.charge_types)
DECLARE @ChargeTypeSppSmpId UNIQUEIDENTIFIER = '01A1F11A-8010-8010-8000-000000000010';
DECLARE @ChargeTypeSppSmaId UNIQUEIDENTIFIER = '01A1F11A-8011-8011-8000-000000000011';
DECLARE @ChargeTypeUniformId UNIQUEIDENTIFIER = '01A1F11A-8012-8012-8000-000000000012';

IF NOT EXISTS (SELECT 1 FROM [fin].[charge_types] WHERE [id] = @ChargeTypeSppSmpId OR ([name] = N'SPP Bulanan SMP' AND [academic_year_id] = @AcademicYearId))
BEGIN
    INSERT INTO [fin].[charge_types] (
        [id], [academic_year_id], [academic_year_name], [name], [frequency],
        [amount], [school_level], [grade], [study_group_id], [study_group_name],
        [status], [created_at], [updated_at]
    )
    VALUES (
        @ChargeTypeSppSmpId, @AcademicYearId, @AcademicYearName, N'SPP Bulanan SMP', 'MONTHLY',
        350000.00, 'SMP', NULL, NULL, NULL,
        'ACTIVE', @Now, @Now
    );
END
ELSE
BEGIN
    SELECT TOP (1) @ChargeTypeSppSmpId = [id] FROM [fin].[charge_types] WHERE [id] = @ChargeTypeSppSmpId OR ([name] = N'SPP Bulanan SMP' AND [academic_year_id] = @AcademicYearId);
END;

IF NOT EXISTS (SELECT 1 FROM [fin].[charge_types] WHERE [id] = @ChargeTypeSppSmaId OR ([name] = N'SPP Bulanan SMA' AND [academic_year_id] = @AcademicYearId))
BEGIN
    INSERT INTO [fin].[charge_types] (
        [id], [academic_year_id], [academic_year_name], [name], [frequency],
        [amount], [school_level], [grade], [study_group_id], [study_group_name],
        [status], [created_at], [updated_at]
    )
    VALUES (
        @ChargeTypeSppSmaId, @AcademicYearId, @AcademicYearName, N'SPP Bulanan SMA', 'MONTHLY',
        450000.00, 'SMA', NULL, NULL, NULL,
        'ACTIVE', @Now, @Now
    );
END
ELSE
BEGIN
    SELECT TOP (1) @ChargeTypeSppSmaId = [id] FROM [fin].[charge_types] WHERE [id] = @ChargeTypeSppSmaId OR ([name] = N'SPP Bulanan SMA' AND [academic_year_id] = @AcademicYearId);
END;

IF NOT EXISTS (SELECT 1 FROM [fin].[charge_types] WHERE [id] = @ChargeTypeUniformId OR ([name] = N'Seragam Sekolah' AND [academic_year_id] = @AcademicYearId))
BEGIN
    INSERT INTO [fin].[charge_types] (
        [id], [academic_year_id], [academic_year_name], [name], [frequency],
        [amount], [school_level], [grade], [study_group_id], [study_group_name],
        [status], [created_at], [updated_at]
    )
    VALUES (
        @ChargeTypeUniformId, @AcademicYearId, @AcademicYearName, N'Seragam Sekolah', 'ONE_TIME',
        250000.00, NULL, NULL, NULL, NULL,
        'ACTIVE', @Now, @Now
    );
END
ELSE
BEGIN
    SELECT TOP (1) @ChargeTypeUniformId = [id] FROM [fin].[charge_types] WHERE [id] = @ChargeTypeUniformId OR ([name] = N'Seragam Sekolah' AND [academic_year_id] = @AcademicYearId);
END;

-- 7. Seed Student Discounts (fin.student_discounts)
DECLARE @DiscountSitiId UNIQUEIDENTIFIER = '01A1F11A-8020-8020-8000-000000000020';
DECLARE @DiscountAhmadId UNIQUEIDENTIFIER = '01A1F11A-8021-8021-8000-000000000021';

IF NOT EXISTS (SELECT 1 FROM [fin].[student_discounts] WHERE [id] = @DiscountSitiId OR ([student_id] = @Student2Id AND [charge_type_id] = @ChargeTypeSppSmpId AND [academic_year_id] = @AcademicYearId))
BEGIN
    INSERT INTO [fin].[student_discounts] (
        [id], [academic_year_id], [academic_year_name], [charge_type_id], [charge_type_name],
        [student_id], [student_name], [student_nis], [discount_type], [discount_value],
        [reason], [is_active], [created_at], [updated_at]
    )
    VALUES (
        @DiscountSitiId, @AcademicYearId, @AcademicYearName, @ChargeTypeSppSmpId, N'SPP Bulanan SMP',
        @Student2Id, @Student2Name, @Student2Nis, 'PERCENTAGE', 20.00,
        N'Diskon Saudara Kandung 20%', 1, @Now, @Now
    );
END;

IF NOT EXISTS (SELECT 1 FROM [fin].[student_discounts] WHERE [id] = @DiscountAhmadId OR ([student_id] = @Student1Id AND [charge_type_id] = @ChargeTypeSppSmpId AND [academic_year_id] = @AcademicYearId))
BEGIN
    INSERT INTO [fin].[student_discounts] (
        [id], [academic_year_id], [academic_year_name], [charge_type_id], [charge_type_name],
        [student_id], [student_name], [student_nis], [discount_type], [discount_value],
        [reason], [is_active], [created_at], [updated_at]
    )
    VALUES (
        @DiscountAhmadId, @AcademicYearId, @AcademicYearName, @ChargeTypeSppSmpId, N'SPP Bulanan SMP',
        @Student1Id, @Student1Name, @Student1Nis, 'FIXED_AMOUNT', 100000.00,
        N'Beasiswa Prestasi Akademik', 1, @Now, @Now
    );
END;

-- 8. Seed Student Charges (fin.student_charges)
DECLARE @ChargeAhmadSppId UNIQUEIDENTIFIER = '01A1F11A-8030-8030-8000-000000000030';
DECLARE @ChargeSitiSppId UNIQUEIDENTIFIER = '01A1F11A-8031-8031-8000-000000000031';
DECLARE @ChargeAhmadUniformId UNIQUEIDENTIFIER = '01A1F11A-8032-8032-8000-000000000032';

IF NOT EXISTS (SELECT 1 FROM [fin].[student_charges] WHERE [id] = @ChargeAhmadSppId OR ([student_id] = @Student1Id AND [charge_type_id] = @ChargeTypeSppSmpId AND [billing_month] = '2026-07'))
BEGIN
    INSERT INTO [fin].[student_charges] (
        [id], [academic_year_id], [academic_year_name], [operational_period_id], [operational_period_name],
        [billing_month], [charge_type_id], [charge_type_name], [charge_frequency],
        [student_id], [student_name], [student_nis], [school_level], [grade],
        [study_group_id], [study_group_name], [description],
        [unit_amount], [quantity], [gross_amount], [discount_amount], [discount_reason],
        [net_amount], [paid_amount], [outstanding_amount], [status],
        [due_date], [cancellation_reason], [cancelled_at], [created_at], [updated_at]
    )
    VALUES (
        @ChargeAhmadSppId, @AcademicYearId, @AcademicYearName, @OperationalPeriodId, @OperationalPeriodName,
        '2026-07', @ChargeTypeSppSmpId, N'SPP Bulanan SMP', 'MONTHLY',
        @Student1Id, @Student1Name, @Student1Nis, 'SMP', '7',
        @StudyGroupId7A, @StudyGroupName7A, N'Tagihan SPP Juli 2026',
        350000.00, 1, 350000.00, 100000.00, N'Beasiswa Prestasi Akademik',
        250000.00, 250000.00, 0.00, 'PAID',
        '2026-07-10', NULL, NULL, @Now, @Now
    );
END
ELSE
BEGIN
    SELECT TOP (1) @ChargeAhmadSppId = [id] FROM [fin].[student_charges] WHERE [id] = @ChargeAhmadSppId OR ([student_id] = @Student1Id AND [charge_type_id] = @ChargeTypeSppSmpId AND [billing_month] = '2026-07');
END;

IF NOT EXISTS (SELECT 1 FROM [fin].[student_charges] WHERE [id] = @ChargeSitiSppId OR ([student_id] = @Student2Id AND [charge_type_id] = @ChargeTypeSppSmpId AND [billing_month] = '2026-07'))
BEGIN
    INSERT INTO [fin].[student_charges] (
        [id], [academic_year_id], [academic_year_name], [operational_period_id], [operational_period_name],
        [billing_month], [charge_type_id], [charge_type_name], [charge_frequency],
        [student_id], [student_name], [student_nis], [school_level], [grade],
        [study_group_id], [study_group_name], [description],
        [unit_amount], [quantity], [gross_amount], [discount_amount], [discount_reason],
        [net_amount], [paid_amount], [outstanding_amount], [status],
        [due_date], [cancellation_reason], [cancelled_at], [created_at], [updated_at]
    )
    VALUES (
        @ChargeSitiSppId, @AcademicYearId, @AcademicYearName, @OperationalPeriodId, @OperationalPeriodName,
        '2026-07', @ChargeTypeSppSmpId, N'SPP Bulanan SMP', 'MONTHLY',
        @Student2Id, @Student2Name, @Student2Nis, 'SMP', '7',
        @StudyGroupId7A, @StudyGroupName7A, N'Tagihan SPP Juli 2026',
        350000.00, 1, 350000.00, 70000.00, N'Diskon Saudara Kandung 20%',
        280000.00, 150000.00, 130000.00, 'PARTIALLY_PAID',
        '2026-07-10', NULL, NULL, @Now, @Now
    );
END
ELSE
BEGIN
    SELECT TOP (1) @ChargeSitiSppId = [id] FROM [fin].[student_charges] WHERE [id] = @ChargeSitiSppId OR ([student_id] = @Student2Id AND [charge_type_id] = @ChargeTypeSppSmpId AND [billing_month] = '2026-07');
END;

IF NOT EXISTS (SELECT 1 FROM [fin].[student_charges] WHERE [id] = @ChargeAhmadUniformId OR ([student_id] = @Student1Id AND [charge_type_id] = @ChargeTypeUniformId AND [billing_month] = '2026-07'))
BEGIN
    INSERT INTO [fin].[student_charges] (
        [id], [academic_year_id], [academic_year_name], [operational_period_id], [operational_period_name],
        [billing_month], [charge_type_id], [charge_type_name], [charge_frequency],
        [student_id], [student_name], [student_nis], [school_level], [grade],
        [study_group_id], [study_group_name], [description],
        [unit_amount], [quantity], [gross_amount], [discount_amount], [discount_reason],
        [net_amount], [paid_amount], [outstanding_amount], [status],
        [due_date], [cancellation_reason], [cancelled_at], [created_at], [updated_at]
    )
    VALUES (
        @ChargeAhmadUniformId, @AcademicYearId, @AcademicYearName, @OperationalPeriodId, @OperationalPeriodName,
        '2026-07', @ChargeTypeUniformId, N'Seragam Sekolah', 'ONE_TIME',
        @Student1Id, @Student1Name, @Student1Nis, 'SMP', '7',
        @StudyGroupId7A, @StudyGroupName7A, N'Pembelian Seragam Sekolah Siswa Baru',
        250000.00, 1, 250000.00, 0.00, NULL,
        250000.00, 0.00, 250000.00, 'UNPAID',
        '2026-07-15', NULL, NULL, @Now, @Now
    );
END
ELSE
BEGIN
    SELECT TOP (1) @ChargeAhmadUniformId = [id] FROM [fin].[student_charges] WHERE [id] = @ChargeAhmadUniformId OR ([student_id] = @Student1Id AND [charge_type_id] = @ChargeTypeUniformId AND [billing_month] = '2026-07');
END;

-- 9. Seed Payment Receipts & Allocations (fin.payment_receipts, fin.payment_allocations)
DECLARE @Receipt1Id UNIQUEIDENTIFIER = '01A1F11A-8040-8040-8000-000000000040';
DECLARE @Receipt1Number NVARCHAR(30) = N'REC/2026/07/0001';
DECLARE @Receipt2Id UNIQUEIDENTIFIER = '01A1F11A-8041-8041-8000-000000000041';
DECLARE @Receipt2Number NVARCHAR(30) = N'REC/2026/07/0002';

IF NOT EXISTS (SELECT 1 FROM [fin].[payment_receipts] WHERE [id] = @Receipt1Id OR [receipt_number] = @Receipt1Number)
BEGIN
    INSERT INTO [fin].[payment_receipts] (
        [id], [receipt_number], [academic_year_id], [academic_year_name],
        [student_id], [student_name], [student_nis], [study_group_id], [study_group_name],
        [payment_date], [payment_method], [bank_reference],
        [total_amount], [allocated_amount], [unallocated_amount],
        [received_by_id], [received_by_name], [notes], [created_at], [updated_at]
    )
    VALUES (
        @Receipt1Id, @Receipt1Number, @AcademicYearId, @AcademicYearName,
        @Student1Id, @Student1Name, @Student1Nis, @StudyGroupId7A, @StudyGroupName7A,
        '2026-07-05', 'CASH', NULL,
        250000.00, 250000.00, 0.00,
        @FinanceUserId, @FinanceUserName, N'Pembayaran lunas SPP Juli tunai di kasir', @Now, @Now
    );
END;

IF NOT EXISTS (SELECT 1 FROM [fin].[payment_receipts] WHERE [id] = @Receipt2Id OR [receipt_number] = @Receipt2Number)
BEGIN
    INSERT INTO [fin].[payment_receipts] (
        [id], [receipt_number], [academic_year_id], [academic_year_name],
        [student_id], [student_name], [student_nis], [study_group_id], [study_group_name],
        [payment_date], [payment_method], [bank_reference],
        [total_amount], [allocated_amount], [unallocated_amount],
        [received_by_id], [received_by_name], [notes], [created_at], [updated_at]
    )
    VALUES (
        @Receipt2Id, @Receipt2Number, @AcademicYearId, @AcademicYearName,
        @Student2Id, @Student2Name, @Student2Nis, @StudyGroupId7A, @StudyGroupName7A,
        '2026-07-06', 'BANK_TRANSFER', N'TRX-BCA-987654',
        200000.00, 150000.00, 50000.00,
        @FinanceUserId, @FinanceUserName, N'Transfer BCA, sisa Rp 50.000 disimpan sebagai deposit', @Now, @Now
    );
END;

DECLARE @Alloc1Id UNIQUEIDENTIFIER = '01A1F11A-8050-8050-8000-000000000050';
DECLARE @Alloc2Id UNIQUEIDENTIFIER = '01A1F11A-8051-8051-8000-000000000051';

IF NOT EXISTS (SELECT 1 FROM [fin].[payment_allocations] WHERE [id] = @Alloc1Id)
BEGIN
    INSERT INTO [fin].[payment_allocations] (
        [id], [payment_receipt_id], [receipt_number], [student_charge_id],
        [charge_type_name], [billing_month], [student_id], [student_name], [student_nis],
        [allocated_amount], [allocation_date], [created_at], [updated_at]
    )
    VALUES (
        @Alloc1Id, @Receipt1Id, @Receipt1Number, @ChargeAhmadSppId,
        N'SPP Bulanan SMP', '2026-07', @Student1Id, @Student1Name, @Student1Nis,
        250000.00, '2026-07-05', @Now, @Now
    );
END;

IF NOT EXISTS (SELECT 1 FROM [fin].[payment_allocations] WHERE [id] = @Alloc2Id)
BEGIN
    INSERT INTO [fin].[payment_allocations] (
        [id], [payment_receipt_id], [receipt_number], [student_charge_id],
        [charge_type_name], [billing_month], [student_id], [student_name], [student_nis],
        [allocated_amount], [allocation_date], [created_at], [updated_at]
    )
    VALUES (
        @Alloc2Id, @Receipt2Id, @Receipt2Number, @ChargeSitiSppId,
        N'SPP Bulanan SMP', '2026-07', @Student2Id, @Student2Name, @Student2Nis,
        150000.00, '2026-07-06', @Now, @Now
    );
END;

-- 10. Seed Receipt Sequences (fin.receipt_sequences)
IF NOT EXISTS (SELECT 1 FROM [fin].[receipt_sequences] WHERE [month] = '2026-07')
BEGIN
    INSERT INTO [fin].[receipt_sequences] ([month], [next_seq])
    VALUES ('2026-07', 3);
END;

-- 11. Seed Behaviour Notes (dev.behaviour_notes)
DECLARE @Note1Id UNIQUEIDENTIFIER = '01A1F11A-8080-8080-8000-000000000080';
DECLARE @Note2Id UNIQUEIDENTIFIER = '01A1F11A-8081-8081-8000-000000000081';

IF NOT EXISTS (SELECT 1 FROM [dev].[behaviour_notes] WHERE [id] = @Note1Id)
BEGIN
    INSERT INTO [dev].[behaviour_notes] (
        [id], [student_id], [student_name], [student_nis], [academic_year_id], [academic_year_name],
        [behaviour_type], [observed_date], [description], [recorder_id], [recorder_name],
        [created_at], [updated_at]
    )
    VALUES (
        @Note1Id, @Student1Id, @Student1Name, @Student1Nis, @AcademicYearId, @AcademicYearName,
        'POSITIVE', '2026-07-08', N'Membantu merapikan laboratorium komputer dan menolong teman yang kesulitan perangkat.',
        @BudiTeacherId, @BudiTeacherName, @Now, @Now
    );
END;

IF NOT EXISTS (SELECT 1 FROM [dev].[behaviour_notes] WHERE [id] = @Note2Id)
BEGIN
    INSERT INTO [dev].[behaviour_notes] (
        [id], [student_id], [student_name], [student_nis], [academic_year_id], [academic_year_name],
        [behaviour_type], [observed_date], [description], [recorder_id], [recorder_name],
        [created_at], [updated_at]
    )
    VALUES (
        @Note2Id, @Student2Id, @Student2Name, @Student2Nis, @AcademicYearId, @AcademicYearName,
        'NEGATIVE', '2026-07-09', N'Terlambat hadir 15 menit pada jam pelajaran pertama tanpa konfirmasi.',
        @BudiTeacherId, @BudiTeacherName, @Now, @Now
    );
END;

-- 12. Seed Discipline Records (dev.discipline_records)
DECLARE @Disc1Id UNIQUEIDENTIFIER = '01A1F11A-8090-8090-8000-000000000090';
DECLARE @Disc2Id UNIQUEIDENTIFIER = '01A1F11A-8091-8091-8000-000000000091';

IF NOT EXISTS (SELECT 1 FROM [dev].[discipline_records] WHERE [id] = @Disc1Id)
BEGIN
    INSERT INTO [dev].[discipline_records] (
        [id], [student_id], [student_name], [student_nis], [academic_year_id], [academic_year_name],
        [incident_date], [violation_type], [description], [sanction],
        [recorder_id], [recorder_name], [created_at], [updated_at]
    )
    VALUES (
        @Disc1Id, @Student1Id, @Student1Name, @Student1Nis, @AcademicYearId, @AcademicYearName,
        '2026-07-10', 'UNIFORM_VIOLATION', N'Lupa mengenakan dasi seragam saat upacara bendera hari Senin.',
        'VERBAL_WARNING', @BudiTeacherId, @BudiTeacherName, @Now, @Now
    );
END;

IF NOT EXISTS (SELECT 1 FROM [dev].[discipline_records] WHERE [id] = @Disc2Id)
BEGIN
    INSERT INTO [dev].[discipline_records] (
        [id], [student_id], [student_name], [student_nis], [academic_year_id], [academic_year_name],
        [incident_date], [violation_type], [description], [sanction],
        [recorder_id], [recorder_name], [created_at], [updated_at]
    )
    VALUES (
        @Disc2Id, @Student2Id, @Student2Name, @Student2Nis, @AcademicYearId, @AcademicYearName,
        '2026-07-11', 'PHYSICAL_ALTERCATION', N'Terlibat pertengkaran fisik di luar gerbang sekolah seusai jam pelajaran.',
        'SUSPENSION', @BudiTeacherId, @BudiTeacherName, @Now, @Now
    );
END;

-- 13. Seed Extracurricular Activities (dev.extracurricular_activities)
DECLARE @ClubPramukaId UNIQUEIDENTIFIER = '01A1F11A-8060-8060-8000-000000000060';
DECLARE @ClubFutsalId UNIQUEIDENTIFIER = '01A1F11A-8061-8061-8000-000000000061';
DECLARE @ClubEnglishId UNIQUEIDENTIFIER = '01A1F11A-8062-8062-8000-000000000062';

IF NOT EXISTS (SELECT 1 FROM [dev].[extracurricular_activities] WHERE [id] = @ClubPramukaId OR [name] = N'Pramuka')
BEGIN
    INSERT INTO [dev].[extracurricular_activities] (
        [id], [name], [description], [supervisor_id], [supervisor_name], [status],
        [created_at], [updated_at]
    )
    VALUES (
        @ClubPramukaId, N'Pramuka', N'Kegiatan kepanduan wajib pembinaan kedisiplinan dan karakter',
        @BudiTeacherId, @BudiTeacherName, 'ACTIVE', @Now, @Now
    );
END
ELSE
BEGIN
    SELECT TOP (1) @ClubPramukaId = [id] FROM [dev].[extracurricular_activities] WHERE [id] = @ClubPramukaId OR [name] = N'Pramuka';
END;

IF NOT EXISTS (SELECT 1 FROM [dev].[extracurricular_activities] WHERE [id] = @ClubFutsalId OR [name] = N'Futsal')
BEGIN
    INSERT INTO [dev].[extracurricular_activities] (
        [id], [name], [description], [supervisor_id], [supervisor_name], [status],
        [created_at], [updated_at]
    )
    VALUES (
        @ClubFutsalId, N'Futsal', N'Klub olahraga futsal dan kebugaran jasmani putra putri',
        @BudiTeacherId, @BudiTeacherName, 'ACTIVE', @Now, @Now
    );
END
ELSE
BEGIN
    SELECT TOP (1) @ClubFutsalId = [id] FROM [dev].[extracurricular_activities] WHERE [id] = @ClubFutsalId OR [name] = N'Futsal';
END;

IF NOT EXISTS (SELECT 1 FROM [dev].[extracurricular_activities] WHERE [id] = @ClubEnglishId OR [name] = N'English Club')
BEGIN
    INSERT INTO [dev].[extracurricular_activities] (
        [id], [name], [description], [supervisor_id], [supervisor_name], [status],
        [created_at], [updated_at]
    )
    VALUES (
        @ClubEnglishId, N'English Club', N'Pengembangan kemampuan percakapan dan debat bahasa Inggris',
        @BudiTeacherId, @BudiTeacherName, 'ACTIVE', @Now, @Now
    );
END
ELSE
BEGIN
    SELECT TOP (1) @ClubEnglishId = [id] FROM [dev].[extracurricular_activities] WHERE [id] = @ClubEnglishId OR [name] = N'English Club';
END;

-- 14. Seed Extracurricular Participations (dev.extracurricular_participations)
DECLARE @PartPramukaAhmadId UNIQUEIDENTIFIER = '01A1F11A-8070-8070-8000-000000000070';
DECLARE @PartEnglishSitiId UNIQUEIDENTIFIER = '01A1F11A-8071-8071-8000-000000000071';
DECLARE @PartFutsalAhmadId UNIQUEIDENTIFIER = '01A1F11A-8072-8072-8000-000000000072';

IF NOT EXISTS (SELECT 1 FROM [dev].[extracurricular_participations] WHERE [id] = @PartPramukaAhmadId OR ([student_id] = @Student1Id AND [activity_id] = @ClubPramukaId AND [academic_year_id] = @AcademicYearId))
BEGIN
    INSERT INTO [dev].[extracurricular_participations] (
        [id], [activity_id], [activity_name], [student_id], [student_name], [student_nis],
        [academic_year_id], [academic_year_name], [status], [joined_date],
        [completed_at], [withdrawn_at], [last_session_log], [last_session_date],
        [created_at], [updated_at]
    )
    VALUES (
        @PartPramukaAhmadId, @ClubPramukaId, N'Pramuka', @Student1Id, @Student1Name, @Student1Nis,
        @AcademicYearId, @AcademicYearName, 'ACTIVE', '2026-07-02',
        NULL, NULL, N'Latihan teknik pionering tiang bendera dan sandi semaphore', '2026-07-12',
        @Now, @Now
    );
END;

IF NOT EXISTS (SELECT 1 FROM [dev].[extracurricular_participations] WHERE [id] = @PartEnglishSitiId OR ([student_id] = @Student2Id AND [activity_id] = @ClubEnglishId AND [academic_year_id] = @AcademicYearId))
BEGIN
    INSERT INTO [dev].[extracurricular_participations] (
        [id], [activity_id], [activity_name], [student_id], [student_name], [student_nis],
        [academic_year_id], [academic_year_name], [status], [joined_date],
        [completed_at], [withdrawn_at], [last_session_log], [last_session_date],
        [created_at], [updated_at]
    )
    VALUES (
        @PartEnglishSitiId, @ClubEnglishId, N'English Club', @Student2Id, @Student2Name, @Student2Nis,
        @AcademicYearId, @AcademicYearName, 'ACTIVE', '2026-07-03',
        NULL, NULL, N'Speaking role play: Ordering in a restaurant and daily polite expressions', '2026-07-13',
        @Now, @Now
    );
END;

IF NOT EXISTS (SELECT 1 FROM [dev].[extracurricular_participations] WHERE [id] = @PartFutsalAhmadId OR ([student_id] = @Student1Id AND [activity_id] = @ClubFutsalId AND [academic_year_id] = @AcademicYearId))
BEGIN
    INSERT INTO [dev].[extracurricular_participations] (
        [id], [activity_id], [activity_name], [student_id], [student_name], [student_nis],
        [academic_year_id], [academic_year_name], [status], [joined_date],
        [completed_at], [withdrawn_at], [last_session_log], [last_session_date],
        [created_at], [updated_at]
    )
    VALUES (
        @PartFutsalAhmadId, @ClubFutsalId, N'Futsal', @Student1Id, @Student1Name, @Student1Nis,
        @AcademicYearId, @AcademicYearName, 'ACTIVE', '2026-07-04',
        NULL, NULL, N'Latihan penguasaan bola, operan pendek, dan formasi 2-2', '2026-07-14',
        @Now, @Now
    );
END;

-- 15. Seed Achievements (dev.achievements)
DECLARE @Achieve1Id UNIQUEIDENTIFIER = '01A1F11A-80A0-80A0-8000-0000000000A0';
DECLARE @Achieve2Id UNIQUEIDENTIFIER = '01A1F11A-80A1-80A1-8000-0000000000A1';

IF NOT EXISTS (SELECT 1 FROM [dev].[achievements] WHERE [id] = @Achieve1Id)
BEGIN
    INSERT INTO [dev].[achievements] (
        [id], [student_id], [student_name], [student_nis], [academic_year_id], [academic_year_name],
        [achievement_type], [event_name], [recognition_level], [achievement_date],
        [description], [recorder_id], [recorder_name], [created_at], [updated_at]
    )
    VALUES (
        @Achieve1Id, @Student1Id, @Student1Name, @Student1Nis, @AcademicYearId, @AcademicYearName,
        'ACADEMIC', N'Olimpiade Sains Nasional Bidang Matematika SMP', 'CITY', '2026-07-15',
        N'Meraih Medali Perak / Juara 2 OSN Matematika Tingkat Kota', @BudiTeacherId, @BudiTeacherName,
        @Now, @Now
    );
END;

IF NOT EXISTS (SELECT 1 FROM [dev].[achievements] WHERE [id] = @Achieve2Id)
BEGIN
    INSERT INTO [dev].[achievements] (
        [id], [student_id], [student_name], [student_nis], [academic_year_id], [academic_year_name],
        [achievement_type], [event_name], [recognition_level], [achievement_date],
        [description], [recorder_id], [recorder_name], [created_at], [updated_at]
    )
    VALUES (
        @Achieve2Id, @Student2Id, @Student2Name, @Student2Nis, @AcademicYearId, @AcademicYearName,
        'SPORTS', N'Kejuaraan Futsal Putri Antar Pelajar Daerah', 'PROVINCE', '2026-07-20',
        N'Juara 1 Tingkat Provinsi dan dinobatkan sebagai Pemain Terbaik Turnamen', @BudiTeacherId, @BudiTeacherName,
        @Now, @Now
    );
END;

-- 16. Seed Report Cards & Entries (acd.report_cards, acd.report_card_entries)
-- Resolve Subject MTK and Subject BING & Subject Stages
DECLARE @MtkSubjId UNIQUEIDENTIFIER = NULL;
DECLARE @BingSubjId UNIQUEIDENTIFIER = NULL;
DECLARE @MathStageId UNIQUEIDENTIFIER = '01A1F11A-7002-7002-8000-000000000002';
DECLARE @EnglishStageId UNIQUEIDENTIFIER = '01A1F11A-7003-7003-8000-000000000003';

SELECT TOP (1) @MtkSubjId = [id] FROM [acd].[subjects] WHERE [code] = N'MTK' OR [name] = N'Matematika';
SELECT TOP (1) @BingSubjId = [id] FROM [acd].[subjects] WHERE [code] = N'BING' OR [name] = N'Bahasa Inggris';
SELECT TOP (1) @MathStageId = [id] FROM [acd].[subject_stages] WHERE [id] = @MathStageId OR [name] = N'Matematika Tingkat 7';
SELECT TOP (1) @EnglishStageId = [id] FROM [acd].[subject_stages] WHERE [id] = @EnglishStageId OR [name] = N'Bahasa Inggris Tingkat 7';

DECLARE @ReportCardAhmadId UNIQUEIDENTIFIER = '01A1F11A-80B0-80B0-8000-0000000000B0';
DECLARE @ReportCardSitiId UNIQUEIDENTIFIER = '01A1F11A-80B1-80B1-8000-0000000000B1';

-- Report Card Ahmad (FINALIZED)
IF NOT EXISTS (SELECT 1 FROM [acd].[report_cards] WHERE [id] = @ReportCardAhmadId OR ([student_id] = @Student1Id AND [operational_period_id] = @OperationalPeriodId))
BEGIN
    INSERT INTO [acd].[report_cards] (
        [id], [student_id], [student_name], [student_nis], [student_nisn],
        [academic_year_id], [academic_year_name], [operational_period_id], [operational_period_name],
        [study_group_id], [study_group_name], [homeroom_teacher_id], [homeroom_teacher_name],
        [status], [finalized_at], [override_reason], [general_remarks],
        [created_at], [updated_at]
    )
    VALUES (
        @ReportCardAhmadId, @Student1Id, @Student1Name, @Student1Nis, N'0081234561',
        @AcademicYearId, @AcademicYearName, @OperationalPeriodId, @OperationalPeriodName,
        @StudyGroupId7A, @StudyGroupName7A, @BudiTeacherId, @BudiTeacherName,
        'FINALIZED', @Now, NULL, N'Siswa menunjukkan pemahaman konseptual yang istimewa, mandiri, dan berkarakter kepemimpinan santun.',
        @Now, @Now
    );
END
ELSE
BEGIN
    SELECT TOP (1) @ReportCardAhmadId = [id] FROM [acd].[report_cards] WHERE [id] = @ReportCardAhmadId OR ([student_id] = @Student1Id AND [operational_period_id] = @OperationalPeriodId);
END;

-- Report Card Siti (DRAFT)
IF NOT EXISTS (SELECT 1 FROM [acd].[report_cards] WHERE [id] = @ReportCardSitiId OR ([student_id] = @Student2Id AND [operational_period_id] = @OperationalPeriodId))
BEGIN
    INSERT INTO [acd].[report_cards] (
        [id], [student_id], [student_name], [student_nis], [student_nisn],
        [academic_year_id], [academic_year_name], [operational_period_id], [operational_period_name],
        [study_group_id], [study_group_name], [homeroom_teacher_id], [homeroom_teacher_name],
        [status], [finalized_at], [override_reason], [general_remarks],
        [created_at], [updated_at]
    )
    VALUES (
        @ReportCardSitiId, @Student2Id, @Student2Name, @Student2Nis, N'0081234562',
        @AcademicYearId, @AcademicYearName, @OperationalPeriodId, @OperationalPeriodName,
        @StudyGroupId7A, @StudyGroupName7A, @BudiTeacherId, @BudiTeacherName,
        'DRAFT', NULL, NULL, NULL,
        @Now, @Now
    );
END
ELSE
BEGIN
    SELECT TOP (1) @ReportCardSitiId = [id] FROM [acd].[report_cards] WHERE [id] = @ReportCardSitiId OR ([student_id] = @Student2Id AND [operational_period_id] = @OperationalPeriodId);
END;

-- Report card entries for Ahmad
DECLARE @EntryAhmadMtkId UNIQUEIDENTIFIER = '01A1F11A-80C0-80C0-8000-0000000000C0';
DECLARE @EntryAhmadBingId UNIQUEIDENTIFIER = '01A1F11A-80C1-80C1-8000-0000000000C1';

IF @MathStageId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [acd].[report_card_entries] WHERE [id] = @EntryAhmadMtkId OR ([report_card_id] = @ReportCardAhmadId AND [subject_stage_id] = @MathStageId))
BEGIN
    INSERT INTO [acd].[report_card_entries] (
        [id], [report_card_id], [subject_id], [subject_name], [subject_code],
        [subject_stage_id], [subject_stage_name], [teacher_id], [teacher_name],
        [numeric_score], [letter_grade], [teacher_notes], [created_at], [updated_at]
    )
    VALUES (
        @EntryAhmadMtkId, @ReportCardAhmadId, ISNULL(@MtkSubjId, '01A1F11A-6004-7004-8000-000000000004'),
        N'Matematika', N'MTK', @MathStageId, N'Matematika Tingkat 7',
        @BudiTeacherId, @BudiTeacherName, 88.50, 'A',
        N'Menguasai konsep aljabar dan bilangan prima dengan sangat baik.',
        @Now, @Now
    );
END;

IF @EnglishStageId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [acd].[report_card_entries] WHERE [id] = @EntryAhmadBingId OR ([report_card_id] = @ReportCardAhmadId AND [subject_stage_id] = @EnglishStageId))
BEGIN
    INSERT INTO [acd].[report_card_entries] (
        [id], [report_card_id], [subject_id], [subject_name], [subject_code],
        [subject_stage_id], [subject_stage_name], [teacher_id], [teacher_name],
        [numeric_score], [letter_grade], [teacher_notes], [created_at], [updated_at]
    )
    VALUES (
        @EntryAhmadBingId, @ReportCardAhmadId, ISNULL(@BingSubjId, '01A1F11A-6007-7007-8000-000000000007'),
        N'Bahasa Inggris', N'BING', @EnglishStageId, N'Bahasa Inggris Tingkat 7',
        @BudiTeacherId, @BudiTeacherName, 86.00, 'A',
        N'Kemampuan menyusun kalimat tanya dan deskripsi lisan sangat ekspresif.',
        @Now, @Now
    );
END;

-- Report card entries for Siti
DECLARE @EntrySitiMtkId UNIQUEIDENTIFIER = '01A1F11A-80C2-80C2-8000-0000000000C2';
DECLARE @EntrySitiBingId UNIQUEIDENTIFIER = '01A1F11A-80C3-80C3-8000-0000000000C3';

IF @MathStageId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [acd].[report_card_entries] WHERE [id] = @EntrySitiMtkId OR ([report_card_id] = @ReportCardSitiId AND [subject_stage_id] = @MathStageId))
BEGIN
    INSERT INTO [acd].[report_card_entries] (
        [id], [report_card_id], [subject_id], [subject_name], [subject_code],
        [subject_stage_id], [subject_stage_name], [teacher_id], [teacher_name],
        [numeric_score], [letter_grade], [teacher_notes], [created_at], [updated_at]
    )
    VALUES (
        @EntrySitiMtkId, @ReportCardSitiId, ISNULL(@MtkSubjId, '01A1F11A-6004-7004-8000-000000000004'),
        N'Matematika', N'MTK', @MathStageId, N'Matematika Tingkat 7',
        @BudiTeacherId, @BudiTeacherName, 76.50, 'B',
        N'Perlu latihan tambahan dalam penyelesaian persamaan aljabar bertingkat.',
        @Now, @Now
    );
END;

IF @EnglishStageId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [acd].[report_card_entries] WHERE [id] = @EntrySitiBingId OR ([report_card_id] = @ReportCardSitiId AND [subject_stage_id] = @EnglishStageId))
BEGIN
    INSERT INTO [acd].[report_card_entries] (
        [id], [report_card_id], [subject_id], [subject_name], [subject_code],
        [subject_stage_id], [subject_stage_name], [teacher_id], [teacher_name],
        [numeric_score], [letter_grade], [teacher_notes], [created_at], [updated_at]
    )
    VALUES (
        @EntrySitiBingId, @ReportCardSitiId, ISNULL(@BingSubjId, '01A1F11A-6007-7007-8000-000000000007'),
        N'Bahasa Inggris', N'BING', @EnglishStageId, N'Bahasa Inggris Tingkat 7',
        @BudiTeacherId, @BudiTeacherName, 84.00, 'B',
        N'Partisipasi aktif dalam kegiatan percakapan dan presentasi kelompok.',
        @Now, @Now
    );
END;

-- 17. Update Ahmad Dahlan Placement academic completion flag (std.student_placements)
UPDATE [std].[student_placements]
   SET [is_academically_completed] = 1,
       [updated_at] = @Now
 WHERE [student_id] = @Student1Id
   AND [academic_year_id] = @AcademicYearId;
