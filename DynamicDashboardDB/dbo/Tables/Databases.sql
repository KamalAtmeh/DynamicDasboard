CREATE TABLE [dbo].[Databases] (
    [DatabaseID]       INT           NOT NULL,
    [Name]             VARCHAR (100) NOT NULL,
    [TypeID]           INT           NOT NULL,
    [ConnectionString] TEXT          NOT NULL,
    [CreatedAt]        DATETIME      DEFAULT (getdate()) NULL,
    PRIMARY KEY CLUSTERED ([DatabaseID] ASC),
    FOREIGN KEY ([TypeID]) REFERENCES [dbo].[DatabaseTypes] ([TypeID])
);


GO
CREATE NONCLUSTERED INDEX [idx_database_type]
    ON [dbo].[Databases]([TypeID] ASC);

