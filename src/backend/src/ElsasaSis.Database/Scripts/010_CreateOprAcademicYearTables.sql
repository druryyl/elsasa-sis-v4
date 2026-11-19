CREATE TABLE [opr].[academic_years] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [name] NVARCHAR(50) NOT NULL,
    [start_date] DATE NOT NULL,
    [end_date] DATE NOT NULL,
    [status] VARCHAR(20) NOT NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_academic_years] PRIMARY KEY CLUSTERED ([id]),
    CONSTRAINT [UQ_academic_years_name] UNIQUE ([name])
);

CREATE NONCLUSTERED INDEX [IX_academic_years_status]
    ON [opr].[academic_years] ([status]);

CREATE TABLE [opr].[operational_periods] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [academic_year_id] UNIQUEIDENTIFIER NOT NULL,
    [name] NVARCHAR(50) NOT NULL,
    [start_date] DATE NOT NULL,
    [end_date] DATE NOT NULL,
    [status] VARCHAR(20) NOT NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_operational_periods] PRIMARY KEY CLUSTERED ([id]),
    CONSTRAINT [UQ_operational_periods_year_name] UNIQUE ([academic_year_id], [name])
);

CREATE NONCLUSTERED INDEX [IX_operational_periods_year_dates]
    ON [opr].[operational_periods] ([academic_year_id], [start_date], [end_date]);

CREATE NONCLUSTERED INDEX [IX_operational_periods_status]
    ON [opr].[operational_periods] ([status]);
