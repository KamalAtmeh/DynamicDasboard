CREATE TABLE [dbo].[Logs] (
    [LogID]            INT          IDENTITY (1, 1) NOT NULL,
    [UserID]           INT          NULL,
    [EventType]        VARCHAR (50) NOT NULL,
    [EventDescription] TEXT         NOT NULL,
    [Timestamp]        DATETIME     DEFAULT (getdate()) NULL,
    PRIMARY KEY CLUSTERED ([LogID] ASC),
    FOREIGN KEY ([UserID]) REFERENCES [dbo].[Users] ([UserID])
);

