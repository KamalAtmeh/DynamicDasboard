CREATE TABLE [dbo].[Results] (
    [ResultID]          INT          NOT NULL,
    [QueryID]           INT          NOT NULL,
    [ResultData]        TEXT         NULL,
    [VisualizationType] VARCHAR (50) NULL,
    [CreatedAt]         DATETIME     DEFAULT (getdate()) NULL,
    PRIMARY KEY CLUSTERED ([ResultID] ASC),
    FOREIGN KEY ([QueryID]) REFERENCES [dbo].[Queries] ([QueryID])
);

