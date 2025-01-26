CREATE TABLE [dbo].[Queries] (
    [QueryID]       INT          NOT NULL,
    [UserID]        INT          NOT NULL,
    [DatabaseID]    INT          NOT NULL,
    [QueryText]     TEXT         NOT NULL,
    [ExecutedSQL]   TEXT         NULL,
    [ExecutionTime] FLOAT (53)   NULL,
    [Status]        VARCHAR (50) NULL,
    [CreatedAt]     DATETIME     DEFAULT (getdate()) NULL,
    PRIMARY KEY CLUSTERED ([QueryID] ASC),
    FOREIGN KEY ([DatabaseID]) REFERENCES [dbo].[Databases] ([DatabaseID]),
    FOREIGN KEY ([UserID]) REFERENCES [dbo].[Users] ([UserID])
);


GO
CREATE NONCLUSTERED INDEX [idx_query_user]
    ON [dbo].[Queries]([UserID] ASC);


GO
CREATE NONCLUSTERED INDEX [idx_query_database]
    ON [dbo].[Queries]([DatabaseID] ASC);

