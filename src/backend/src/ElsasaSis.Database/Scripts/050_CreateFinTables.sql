CREATE TABLE [fin].[charge_types] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [academic_year_id] UNIQUEIDENTIFIER NOT NULL,
    [academic_year_name] NVARCHAR(50) NOT NULL,
    [name] NVARCHAR(100) NOT NULL,
    [frequency] VARCHAR(20) NOT NULL,
    [amount] DECIMAL(18,2) NOT NULL,
    [school_level] VARCHAR(10) NULL,
    [grade] VARCHAR(20) NULL,
    [study_group_id] UNIQUEIDENTIFIER NULL,
    [study_group_name] NVARCHAR(50) NULL,
    [status] VARCHAR(20) NOT NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_charge_types] PRIMARY KEY CLUSTERED ([id])
);

CREATE NONCLUSTERED INDEX [IX_charge_types_year_status]
    ON [fin].[charge_types] ([academic_year_id], [status]);

CREATE TABLE [fin].[student_discounts] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [academic_year_id] UNIQUEIDENTIFIER NOT NULL,
    [academic_year_name] NVARCHAR(50) NOT NULL,
    [charge_type_id] UNIQUEIDENTIFIER NOT NULL,
    [charge_type_name] NVARCHAR(100) NOT NULL,
    [student_id] UNIQUEIDENTIFIER NOT NULL,
    [student_name] NVARCHAR(150) NOT NULL,
    [student_nis] NVARCHAR(30) NOT NULL,
    [discount_type] VARCHAR(20) NOT NULL,
    [discount_value] DECIMAL(18,2) NOT NULL,
    [reason] NVARCHAR(500) NOT NULL,
    [is_active] BIT NOT NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_student_discounts] PRIMARY KEY CLUSTERED ([id]),
    CONSTRAINT [UQ_student_discounts_student_charge_year] UNIQUE ([student_id], [charge_type_id], [academic_year_id])
);

CREATE NONCLUSTERED INDEX [IX_student_discounts_charge_type]
    ON [fin].[student_discounts] ([charge_type_id], [is_active]);

CREATE NONCLUSTERED INDEX [IX_student_discounts_student]
    ON [fin].[student_discounts] ([student_id], [academic_year_id]);

CREATE TABLE [fin].[student_charges] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [academic_year_id] UNIQUEIDENTIFIER NOT NULL,
    [academic_year_name] NVARCHAR(50) NOT NULL,
    [operational_period_id] UNIQUEIDENTIFIER NULL,
    [operational_period_name] NVARCHAR(50) NULL,
    [billing_month] CHAR(7) NULL,
    [charge_type_id] UNIQUEIDENTIFIER NOT NULL,
    [charge_type_name] NVARCHAR(100) NOT NULL,
    [charge_frequency] VARCHAR(20) NOT NULL,
    [student_id] UNIQUEIDENTIFIER NOT NULL,
    [student_name] NVARCHAR(150) NOT NULL,
    [student_nis] NVARCHAR(30) NOT NULL,
    [school_level] VARCHAR(10) NOT NULL,
    [grade] VARCHAR(20) NOT NULL,
    [study_group_id] UNIQUEIDENTIFIER NULL,
    [study_group_name] NVARCHAR(50) NULL,
    [description] NVARCHAR(500) NULL,
    [unit_amount] DECIMAL(18,2) NOT NULL,
    [quantity] INT NOT NULL,
    [gross_amount] DECIMAL(18,2) NOT NULL,
    [discount_amount] DECIMAL(18,2) NOT NULL,
    [discount_reason] NVARCHAR(500) NULL,
    [net_amount] DECIMAL(18,2) NOT NULL,
    [paid_amount] DECIMAL(18,2) NOT NULL,
    [outstanding_amount] DECIMAL(18,2) NOT NULL,
    [status] VARCHAR(20) NOT NULL,
    [due_date] DATE NULL,
    [cancellation_reason] NVARCHAR(500) NULL,
    [cancelled_at] DATETIME2(3) NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_student_charges] PRIMARY KEY CLUSTERED ([id])
);

CREATE UNIQUE NONCLUSTERED INDEX [UQ_student_charges_monthly]
    ON [fin].[student_charges] ([student_id], [charge_type_id], [billing_month])
    WHERE [billing_month] IS NOT NULL AND [status] <> 'CANCELLED';

CREATE UNIQUE NONCLUSTERED INDEX [UQ_student_charges_yearly]
    ON [fin].[student_charges] ([student_id], [charge_type_id], [academic_year_id])
    WHERE [charge_frequency] = 'YEARLY' AND [status] <> 'CANCELLED';

CREATE UNIQUE NONCLUSTERED INDEX [UQ_student_charges_semester]
    ON [fin].[student_charges] ([student_id], [charge_type_id], [operational_period_id])
    WHERE [charge_frequency] = 'SEMESTER' AND [operational_period_id] IS NOT NULL AND [status] <> 'CANCELLED';

CREATE NONCLUSTERED INDEX [IX_student_charges_student_status]
    ON [fin].[student_charges] ([student_id], [status]);

CREATE NONCLUSTERED INDEX [IX_student_charges_period_group]
    ON [fin].[student_charges] ([academic_year_id], [operational_period_id], [study_group_id], [status]);

CREATE TABLE [fin].[payment_receipts] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [receipt_number] NVARCHAR(30) NOT NULL,
    [academic_year_id] UNIQUEIDENTIFIER NOT NULL,
    [academic_year_name] NVARCHAR(50) NOT NULL,
    [student_id] UNIQUEIDENTIFIER NOT NULL,
    [student_name] NVARCHAR(150) NOT NULL,
    [student_nis] NVARCHAR(30) NOT NULL,
    [study_group_id] UNIQUEIDENTIFIER NULL,
    [study_group_name] NVARCHAR(50) NULL,
    [payment_date] DATE NOT NULL,
    [payment_method] VARCHAR(30) NOT NULL,
    [bank_reference] NVARCHAR(100) NULL,
    [total_amount] DECIMAL(18,2) NOT NULL,
    [allocated_amount] DECIMAL(18,2) NOT NULL,
    [unallocated_amount] DECIMAL(18,2) NOT NULL,
    [received_by_id] UNIQUEIDENTIFIER NOT NULL,
    [received_by_name] NVARCHAR(150) NOT NULL,
    [notes] NVARCHAR(500) NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_payment_receipts] PRIMARY KEY CLUSTERED ([id]),
    CONSTRAINT [UQ_payment_receipts_receipt_number] UNIQUE ([receipt_number])
);

CREATE NONCLUSTERED INDEX [IX_payment_receipts_student_date]
    ON [fin].[payment_receipts] ([student_id], [payment_date]);

CREATE TABLE [fin].[payment_allocations] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [payment_receipt_id] UNIQUEIDENTIFIER NOT NULL,
    [receipt_number] NVARCHAR(30) NOT NULL,
    [student_charge_id] UNIQUEIDENTIFIER NOT NULL,
    [charge_type_name] NVARCHAR(100) NOT NULL,
    [billing_month] CHAR(7) NULL,
    [student_id] UNIQUEIDENTIFIER NOT NULL,
    [student_name] NVARCHAR(150) NOT NULL,
    [student_nis] NVARCHAR(30) NOT NULL,
    [allocated_amount] DECIMAL(18,2) NOT NULL,
    [allocation_date] DATE NOT NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_payment_allocations] PRIMARY KEY CLUSTERED ([id])
);

CREATE NONCLUSTERED INDEX [IX_payment_allocations_receipt]
    ON [fin].[payment_allocations] ([payment_receipt_id]);

CREATE NONCLUSTERED INDEX [IX_payment_allocations_charge]
    ON [fin].[payment_allocations] ([student_charge_id]);

CREATE NONCLUSTERED INDEX [IX_payment_allocations_student]
    ON [fin].[payment_allocations] ([student_id], [allocation_date]);
