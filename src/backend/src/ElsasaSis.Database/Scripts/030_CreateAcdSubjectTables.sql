CREATE TABLE [acd].[subjects] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [name] NVARCHAR(100) NOT NULL,
    [code] NVARCHAR(30) NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_subjects] PRIMARY KEY CLUSTERED ([id]),
    CONSTRAINT [UQ_subjects_name] UNIQUE ([name])
);

CREATE UNIQUE NONCLUSTERED INDEX [UQ_subjects_code]
    ON [acd].[subjects] ([code])
    WHERE [code] IS NOT NULL;
GO

DECLARE @Now DATETIME2(3) = CAST(SYSUTCDATETIME() AS DATETIME2(3));

IF NOT EXISTS (SELECT 1 FROM [acd].[subjects] WHERE [code] = N'PA-BP' OR [name] = N'Pendidikan Agama dan Budi Pekerti')
BEGIN
    INSERT INTO [acd].[subjects] ([id], [name], [code], [created_at], [updated_at])
    VALUES ('01A1F11A-6001-7001-8000-000000000001', N'Pendidikan Agama dan Budi Pekerti', N'PA-BP', @Now, @Now);
END;

IF NOT EXISTS (SELECT 1 FROM [acd].[subjects] WHERE [code] = N'PPKN' OR [name] = N'Pendidikan Pancasila dan Kewarganegaraan')
BEGIN
    INSERT INTO [acd].[subjects] ([id], [name], [code], [created_at], [updated_at])
    VALUES ('01A1F11A-6002-7002-8000-000000000002', N'Pendidikan Pancasila dan Kewarganegaraan', N'PPKN', @Now, @Now);
END;

IF NOT EXISTS (SELECT 1 FROM [acd].[subjects] WHERE [code] = N'BIND' OR [name] = N'Bahasa Indonesia')
BEGIN
    INSERT INTO [acd].[subjects] ([id], [name], [code], [created_at], [updated_at])
    VALUES ('01A1F11A-6003-7003-8000-000000000003', N'Bahasa Indonesia', N'BIND', @Now, @Now);
END;

IF NOT EXISTS (SELECT 1 FROM [acd].[subjects] WHERE [code] = N'MTK' OR [name] = N'Matematika')
BEGIN
    INSERT INTO [acd].[subjects] ([id], [name], [code], [created_at], [updated_at])
    VALUES ('01A1F11A-6004-7004-8000-000000000004', N'Matematika', N'MTK', @Now, @Now);
END;

IF NOT EXISTS (SELECT 1 FROM [acd].[subjects] WHERE [code] = N'IPA' OR [name] = N'Ilmu Pengetahuan Alam')
BEGIN
    INSERT INTO [acd].[subjects] ([id], [name], [code], [created_at], [updated_at])
    VALUES ('01A1F11A-6005-7005-8000-000000000005', N'Ilmu Pengetahuan Alam', N'IPA', @Now, @Now);
END;

IF NOT EXISTS (SELECT 1 FROM [acd].[subjects] WHERE [code] = N'IPS' OR [name] = N'Ilmu Pengetahuan Sosial')
BEGIN
    INSERT INTO [acd].[subjects] ([id], [name], [code], [created_at], [updated_at])
    VALUES ('01A1F11A-6006-7006-8000-000000000006', N'Ilmu Pengetahuan Sosial', N'IPS', @Now, @Now);
END;

IF NOT EXISTS (SELECT 1 FROM [acd].[subjects] WHERE [code] = N'BING' OR [name] = N'Bahasa Inggris')
BEGIN
    INSERT INTO [acd].[subjects] ([id], [name], [code], [created_at], [updated_at])
    VALUES ('01A1F11A-6007-7007-8000-000000000007', N'Bahasa Inggris', N'BING', @Now, @Now);
END;

IF NOT EXISTS (SELECT 1 FROM [acd].[subjects] WHERE [code] = N'SNB' OR [name] = N'Seni Budaya')
BEGIN
    INSERT INTO [acd].[subjects] ([id], [name], [code], [created_at], [updated_at])
    VALUES ('01A1F11A-6008-7008-8000-000000000008', N'Seni Budaya', N'SNB', @Now, @Now);
END;

IF NOT EXISTS (SELECT 1 FROM [acd].[subjects] WHERE [code] = N'PJOK' OR [name] = N'Pendidikan Jasmani, Olahraga, dan Kesehatan')
BEGIN
    INSERT INTO [acd].[subjects] ([id], [name], [code], [created_at], [updated_at])
    VALUES ('01A1F11A-6009-7009-8000-000000000009', N'Pendidikan Jasmani, Olahraga, dan Kesehatan', N'PJOK', @Now, @Now);
END;

IF NOT EXISTS (SELECT 1 FROM [acd].[subjects] WHERE [code] = N'INF' OR [name] = N'Informatika')
BEGIN
    INSERT INTO [acd].[subjects] ([id], [name], [code], [created_at], [updated_at])
    VALUES ('01A1F11A-600A-700A-8000-00000000000A', N'Informatika', N'INF', @Now, @Now);
END;
