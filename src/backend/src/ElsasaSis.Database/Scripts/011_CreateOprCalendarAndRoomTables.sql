CREATE TABLE [opr].[calendar_entries] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [academic_year_id] UNIQUEIDENTIFIER NOT NULL,
    [entry_date] DATE NOT NULL,
    [day_type] VARCHAR(20) NOT NULL,
    [description] NVARCHAR(500) NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_calendar_entries] PRIMARY KEY CLUSTERED ([id]),
    CONSTRAINT [UQ_calendar_entries_year_date] UNIQUE ([academic_year_id], [entry_date])
);

CREATE NONCLUSTERED INDEX [IX_calendar_entries_year_type]
    ON [opr].[calendar_entries] ([academic_year_id], [day_type], [entry_date]);

CREATE TABLE [opr].[rooms] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [name] NVARCHAR(100) NOT NULL,
    [room_type] VARCHAR(20) NOT NULL,
    [capacity] INT NOT NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_rooms] PRIMARY KEY CLUSTERED ([id]),
    CONSTRAINT [UQ_rooms_name] UNIQUE ([name])
);
