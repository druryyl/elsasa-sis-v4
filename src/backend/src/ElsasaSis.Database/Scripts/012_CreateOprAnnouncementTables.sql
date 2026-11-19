CREATE TABLE [opr].[announcements] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [academic_year_id] UNIQUEIDENTIFIER NOT NULL,
    [title] NVARCHAR(255) NOT NULL,
    [content] NVARCHAR(MAX) NOT NULL,
    [author_id] UNIQUEIDENTIFIER NOT NULL,
    [author_name] NVARCHAR(150) NOT NULL,
    [target_audience] VARCHAR(20) NOT NULL,
    [status] VARCHAR(20) NOT NULL,
    [published_at] DATETIME2(3) NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_announcements] PRIMARY KEY CLUSTERED ([id])
);

CREATE NONCLUSTERED INDEX [IX_announcements_feed]
    ON [opr].[announcements] ([academic_year_id], [status], [target_audience], [published_at] DESC);

CREATE NONCLUSTERED INDEX [IX_announcements_author]
    ON [opr].[announcements] ([academic_year_id], [author_id], [created_at] DESC);
