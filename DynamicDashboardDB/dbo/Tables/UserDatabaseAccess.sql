CREATE TABLE [dbo].[UserDatabaseAccess] (
    [UserDatabaseAccessID] INT NOT NULL,
    [UserID]               INT NOT NULL,
    [DatabaseID]           INT NOT NULL,
    PRIMARY KEY CLUSTERED ([UserDatabaseAccessID] ASC),
    FOREIGN KEY ([DatabaseID]) REFERENCES [dbo].[Databases] ([DatabaseID]),
    FOREIGN KEY ([UserID]) REFERENCES [dbo].[Users] ([UserID])
);

