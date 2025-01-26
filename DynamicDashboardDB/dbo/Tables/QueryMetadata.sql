CREATE TABLE [dbo].[QueryMetadata] (
    [QueryMetadataID] INT NOT NULL,
    [QueryID]         INT NOT NULL,
    [MetadataID]      INT NOT NULL,
    PRIMARY KEY CLUSTERED ([QueryMetadataID] ASC),
    FOREIGN KEY ([MetadataID]) REFERENCES [dbo].[Metadata] ([MetadataID]),
    FOREIGN KEY ([QueryID]) REFERENCES [dbo].[Queries] ([QueryID])
);

