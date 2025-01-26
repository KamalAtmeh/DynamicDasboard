CREATE TABLE [dbo].[QueryLogs] (
    [QueryID]      INT            IDENTITY (1, 1) NOT NULL,
    [QueryText]    NVARCHAR (MAX) NOT NULL,
    [ExecutedAt]   DATETIME       DEFAULT (getdate()) NOT NULL,
    [ExecutedBy]   INT            NULL,
    [DatabaseType] NVARCHAR (50)  NOT NULL,
    [Result]       NVARCHAR (MAX) NULL,
    PRIMARY KEY CLUSTERED ([QueryID] ASC)
);

