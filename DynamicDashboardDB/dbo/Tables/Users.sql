CREATE TABLE [dbo].[Users] (
    [UserID]           INT           NOT NULL,
    [Username]         VARCHAR (100) NOT NULL,
    [PasswordHash]     VARCHAR (255) NOT NULL,
    [RoleID]           INT           NOT NULL,
    [AllowedDatabases] TEXT          NULL,
    [CreatedAt]        DATETIME      DEFAULT (getdate()) NULL,
    PRIMARY KEY CLUSTERED ([UserID] ASC),
    FOREIGN KEY ([RoleID]) REFERENCES [dbo].[UserRoles] ([RoleID])
);


GO
CREATE NONCLUSTERED INDEX [idx_user_role]
    ON [dbo].[Users]([RoleID] ASC);

