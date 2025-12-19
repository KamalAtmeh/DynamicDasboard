using System;
using System.Collections.Generic;

namespace DynamicDashboardCommon.Models
{
    /// <summary>
    /// Represents a template for creating dashboard components.
    /// Templates are predefined component configurations that users can drag onto the canvas.
    /// </summary>
    public class ComponentTemplate
    {
        /// <summary>
        /// Gets or sets the unique identifier for the template.
        /// </summary>
        public int TemplateID { get; set; }

        /// <summary>
        /// Gets or sets the template title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the template description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the data viewing type ID.
        /// </summary>
        public int DataViewingTypeID { get; set; }

        /// <summary>
        /// Gets or sets the FontAwesome icon class (without 'fa-' prefix).
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets the category for grouping templates.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets the default visualization configuration JSON.
        /// </summary>
        public string VisualizationConfig { get; set; }

        /// <summary>
        /// Gets or sets the default SQL query template.
        /// </summary>
        public string DefaultQuery { get; set; }

        /// <summary>
        /// Gets or sets the default grid width.
        /// </summary>
        public int DefaultGridWidth { get; set; } = 6;

        /// <summary>
        /// Gets or sets the default grid height.
        /// </summary>
        public int DefaultGridHeight { get; set; } = 4;

        /// <summary>
        /// Gets or sets whether this template is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the display order within the category.
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets the thumbnail image URL.
        /// </summary>
        public string ThumbnailUrl { get; set; }

        /// <summary>
        /// Gets or sets the tags for searchability.
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets when this template was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets when this template was last updated.
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the minimum required database version for this template.
        /// </summary>
        public string MinDatabaseVersion { get; set; }

        /// <summary>
        /// Gets or sets the usage count for analytics.
        /// </summary>
        public int UsageCount { get; set; }
    }

    /// <summary>
    /// Represents a data source available for components.
    /// </summary>
    public class ComponentDataSource
    {
        /// <summary>
        /// Gets or sets the data source ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the data source name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the database type.
        /// </summary>
        public string DatabaseType { get; set; }

        /// <summary>
        /// Gets or sets whether the data source is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the schema information.
        /// </summary>
        public string SchemaInfo { get; set; }
    }

    /// <summary>
    /// Request model for validating a SQL query.
    /// </summary>
    public class QueryValidationRequest
    {
        /// <summary>
        /// Gets or sets the SQL query to validate.
        /// </summary>
        public string QueryText { get; set; }

        /// <summary>
        /// Gets or sets the database ID to validate against.
        /// </summary>
        public int DatabaseId { get; set; }

        /// <summary>
        /// Gets or sets whether to return sample data.
        /// </summary>
        public bool IncludeSampleData { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum rows to return for sample data.
        /// </summary>
        public int MaxSampleRows { get; set; } = 10;
    }

    /// <summary>
    /// Response model for query validation.
    /// </summary>
    public class QueryValidationResponse
    {
        /// <summary>
        /// Gets or sets whether the query is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the validation message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the error details if validation failed.
        /// </summary>
        public string ErrorDetails { get; set; }

        /// <summary>
        /// Gets or sets the sample data if requested.
        /// </summary>
        public List<Dictionary<string, object>> SampleData { get; set; }

        /// <summary>
        /// Gets or sets the column information.
        /// </summary>
        public List<QueryColumnInfo> Columns { get; set; }

        /// <summary>
        /// Gets or sets the estimated row count.
        /// </summary>
        public long? EstimatedRowCount { get; set; }

        /// <summary>
        /// Gets or sets the execution time in milliseconds.
        /// </summary>
        public long ExecutionTimeMs { get; set; }
    }

    /// <summary>
    /// Information about a query result column.
    /// </summary>
    public class QueryColumnInfo
    {
        /// <summary>
        /// Gets or sets the column name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the column data type.
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// Gets or sets whether the column is nullable.
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// Gets or sets the column ordinal position.
        /// </summary>
        public int Ordinal { get; set; }
    }

    /// <summary>
    /// Request model for generating SQL from natural language.
    /// </summary>
    public class QueryGenerationRequest
    {
        /// <summary>
        /// Gets or sets the natural language query intent.
        /// </summary>
        public string QueryIntent { get; set; }

        /// <summary>
        /// Gets or sets the database ID.
        /// </summary>
        public int DatabaseId { get; set; }

        /// <summary>
        /// Gets or sets the component type for context.
        /// </summary>
        public int DataViewingTypeID { get; set; }

        /// <summary>
        /// Gets or sets additional context for the LLM.
        /// </summary>
        public string AdditionalContext { get; set; }
    }

    /// <summary>
    /// Response model for query generation.
    /// </summary>
    public class QueryGenerationResponse
    {
        /// <summary>
        /// Gets or sets whether the generation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the generated SQL query.
        /// </summary>
        public string GeneratedQuery { get; set; }

        /// <summary>
        /// Gets or sets the explanation of the generated query.
        /// </summary>
        public string Explanation { get; set; }

        /// <summary>
        /// Gets or sets any warnings or suggestions.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the error message if generation failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets suggested alternative queries.
        /// </summary>
        public List<string> AlternativeQueries { get; set; } = new List<string>();
    }

    /// <summary>
    /// Color scheme definition for visualizations.
    /// </summary>
    public class ColorScheme
    {
        /// <summary>
        /// Gets or sets the scheme key/identifier.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the list of colors in this scheme.
        /// </summary>
        public List<string> Colors { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets whether this is a dark theme scheme.
        /// </summary>
        public bool IsDarkTheme { get; set; }
    }

    /// <summary>
    /// Static class containing predefined color schemes.
    /// </summary>
    public static class ColorSchemes
    {
        /// <summary>
        /// Gets all available color schemes.
        /// </summary>
        public static List<ColorScheme> All => new List<ColorScheme>
        {
            new ColorScheme
            {
                Key = "default",
                Name = "Default",
                Colors = new List<string> { "#667eea", "#764ba2", "#f093fb", "#f5576c", "#4facfe", "#00f2fe" }
            },
            new ColorScheme
            {
                Key = "ocean",
                Name = "Ocean",
                Colors = new List<string> { "#4facfe", "#00f2fe", "#43e97b", "#38f9d7", "#667eea", "#764ba2" }
            },
            new ColorScheme
            {
                Key = "sunset",
                Name = "Sunset",
                Colors = new List<string> { "#fa709a", "#fee140", "#f7971e", "#ffd200", "#ff6b6b", "#feca57" }
            },
            new ColorScheme
            {
                Key = "forest",
                Name = "Forest",
                Colors = new List<string> { "#11998e", "#38ef7d", "#56ab2f", "#a8e063", "#134e5e", "#71b280" }
            },
            new ColorScheme
            {
                Key = "berry",
                Name = "Berry",
                Colors = new List<string> { "#8e2de2", "#4a00e0", "#bc4e9c", "#f80759", "#ee0979", "#ff6a00" }
            },
            new ColorScheme
            {
                Key = "monochrome",
                Name = "Monochrome",
                Colors = new List<string> { "#2d3748", "#4a5568", "#718096", "#a0aec0", "#cbd5e0", "#e2e8f0" }
            },
            new ColorScheme
            {
                Key = "pastel",
                Name = "Pastel",
                Colors = new List<string> { "#a8d8ea", "#aa96da", "#fcbad3", "#ffffd2", "#b5eaea", "#f3d9dc" }
            }
        };

        /// <summary>
        /// Gets a color scheme by key.
        /// </summary>
        /// <param name="key">The scheme key.</param>
        /// <returns>The color scheme or default if not found.</returns>
        public static ColorScheme GetByKey(string key)
        {
            return All.Find(s => s.Key.Equals(key, StringComparison.OrdinalIgnoreCase)) ?? All[0];
        }
    }
}