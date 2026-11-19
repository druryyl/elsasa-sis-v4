CREATE TABLE [outbox].[messages] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [module_name] VARCHAR(50) NOT NULL,
    [event_type] VARCHAR(150) NOT NULL,
    [payload] NVARCHAR(MAX) NOT NULL,
    [occurred_at] DATETIME2(3) NOT NULL,
    [processed_at] DATETIME2(3) NULL,
    [retry_count] INT NOT NULL,
    [error] NVARCHAR(MAX) NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_outbox_messages] PRIMARY KEY CLUSTERED ([id])
);

CREATE NONCLUSTERED INDEX [IX_outbox_messages_unprocessed]
    ON [outbox].[messages] ([processed_at], [occurred_at]);
