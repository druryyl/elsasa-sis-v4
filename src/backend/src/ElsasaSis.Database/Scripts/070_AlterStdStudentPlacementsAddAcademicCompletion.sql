IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[std].[student_placements]') 
      AND name = N'is_academically_completed'
)
BEGIN
    ALTER TABLE [std].[student_placements] 
        ADD [is_academically_completed] BIT NOT NULL 
        CONSTRAINT [DF_student_placements_is_academically_completed] DEFAULT 0;
END
ELSE IF NOT EXISTS (
    SELECT 1 FROM sys.default_constraints
    WHERE parent_object_id = OBJECT_ID(N'[std].[student_placements]')
      AND parent_column_id = COLUMNPROPERTY(OBJECT_ID(N'[std].[student_placements]'), 'is_academically_completed', 'ColumnId')
)
BEGIN
    ALTER TABLE [std].[student_placements]
        ADD CONSTRAINT [DF_student_placements_is_academically_completed] DEFAULT 0 FOR [is_academically_completed];
END;
