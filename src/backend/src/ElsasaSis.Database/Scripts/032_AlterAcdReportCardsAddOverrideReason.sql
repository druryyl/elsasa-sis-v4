IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[acd].[report_cards]') 
      AND name = N'override_reason'
)
BEGIN
    ALTER TABLE [acd].[report_cards] ADD [override_reason] NVARCHAR(500) NULL;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[acd].[report_cards]') 
      AND name = N'general_remarks'
)
BEGIN
    ALTER TABLE [acd].[report_cards] ADD [general_remarks] NVARCHAR(1000) NULL;
END;
