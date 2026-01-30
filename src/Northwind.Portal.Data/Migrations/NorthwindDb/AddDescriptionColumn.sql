-- Manual SQL script to add Description column to Products table
-- Run this if the migration doesn't apply automatically

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND name = 'Description')
BEGIN
    ALTER TABLE [Products]
    ADD [Description] nvarchar(500) NULL;
END
GO
