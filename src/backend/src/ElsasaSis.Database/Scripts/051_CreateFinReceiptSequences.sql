CREATE TABLE [fin].[receipt_sequences] (
    [month] CHAR(7) NOT NULL,
    [next_seq] INT NOT NULL,
    CONSTRAINT [PK_receipt_sequences] PRIMARY KEY CLUSTERED ([month])
);
