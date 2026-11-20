IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[fin].[student_charges]') 
      AND name = N'cancellation_reason'
)
BEGIN
    ALTER TABLE [fin].[student_charges] ADD [cancellation_reason] NVARCHAR(500) NULL;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[fin].[student_charges]') 
      AND name = N'cancelled_at'
)
BEGIN
    ALTER TABLE [fin].[student_charges] ADD [cancelled_at] DATETIME2(3) NULL;
END;
