IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dev].[extracurricular_participations]') 
      AND name = N'last_session_log'
)
BEGIN
    ALTER TABLE [dev].[extracurricular_participations] ADD [last_session_log] NVARCHAR(1000) NULL;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dev].[extracurricular_participations]') 
      AND name = N'last_session_date'
)
BEGIN
    ALTER TABLE [dev].[extracurricular_participations] ADD [last_session_date] DATE NULL;
END;
