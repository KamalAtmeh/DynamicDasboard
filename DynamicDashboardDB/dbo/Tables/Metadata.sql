CREATE TABLE [dbo].[Metadata] (
    [MetadataID]       INT           NOT NULL,
    [DatabaseID]       INT           NOT NULL,
    [TableName]        VARCHAR (100) NOT NULL,
    [ColumnName]       VARCHAR (100) NOT NULL,
    [Description]      TEXT          NULL,
    [RelationshipType] VARCHAR (50)  NULL,
    [RelatedTable]     VARCHAR (100) NULL,
    [RelatedColumn]    VARCHAR (100) NULL,
    PRIMARY KEY CLUSTERED ([MetadataID] ASC),
    FOREIGN KEY ([DatabaseID]) REFERENCES [dbo].[Databases] ([DatabaseID])
);


GO
CREATE NONCLUSTERED INDEX [idx_metadata_database]
    ON [dbo].[Metadata]([DatabaseID] ASC);

