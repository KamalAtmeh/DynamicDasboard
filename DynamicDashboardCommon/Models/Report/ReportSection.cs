using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DynamicDashboardCommon.Models
{
    /// <summary>
    /// Represents a section within a report.
    /// Each section can contain data (table/chart) or text (executive summary).
    /// </summary>
    public class ReportSection
    {
        #region Primary Properties

        /// <summary>
        /// Gets or sets the unique identifier for the section.
        /// </summary>
        [Key]
        public int SectionID { get; set; }

        /// <summary>
        /// Gets or sets the parent report ID.
        /// </summary>
        [Required]
        public int ReportID { get; set; }

        /// <summary>
        /// Gets or sets the section title.
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the section description or subtitle.
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the section type.
        /// </summary>
        public ReportSectionTypeEnum SectionType { get; set; } = ReportSectionTypeEnum.DataTable;

        /// <summary>
        /// Gets or sets the display order of this section within the report.
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets whether this section is visible.
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Gets or sets whether this section is expanded in the UI.
        /// </summary>
        public bool IsExpanded { get; set; } = true;

        #endregion

        #region Query Properties

        /// <summary>
        /// Gets or sets the SQL query for this section.
        /// Only applicable for DataTable and Chart sections.
        /// </summary>
        public string QueryText { get; set; }

        /// <summary>
        /// Gets or sets the natural language description of what this section displays.
        /// Used for AI generation and user understanding.
        /// </summary>
        [StringLength(1000)]
        public string QueryIntent { get; set; }

        #endregion

        #region Content Properties

        /// <summary>
        /// Gets or sets the text content for text-based sections (ExecutiveSummary, TextBlock).
        /// </summary>
        public string TextContent { get; set; }

        /// <summary>
        /// Gets or sets the column configuration as JSON.
        /// Defines which columns to show, their order, formatting, and conditional rules.
        /// </summary>
        public string ColumnConfiguration { get; set; }

        /// <summary>
        /// Gets or sets the visualization configuration as JSON.
        /// Used when section is converted to chart.
        /// </summary>
        public string VisualizationConfig { get; set; }

        /// <summary>
        /// Gets or sets the chart type if this section is displayed as a chart.
        /// </summary>
        [StringLength(50)]
        public string ChartType { get; set; }

        /// <summary>
        /// Gets or sets whether this section is currently displayed as a chart.
        /// </summary>
        public bool IsDisplayedAsChart { get; set; } = false;

        #endregion

        #region Audit Properties

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the last modification timestamp.
        /// </summary>
        public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Gets or sets the parent report (navigation property).
        /// </summary>
        [ForeignKey("ReportID")]
        [JsonIgnore]
        public virtual Report Report { get; set; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the section type display name.
        /// </summary>
        [NotMapped]
        public string SectionTypeName => SectionType.ToString();

        /// <summary>
        /// Gets the icon for this section type.
        /// </summary>
        [NotMapped]
        public string SectionIcon => SectionType switch
        {
            ReportSectionTypeEnum.ExecutiveSummary => "fa-file-alt",
            ReportSectionTypeEnum.DataTable => "fa-table",
            ReportSectionTypeEnum.Chart => "fa-chart-bar",
            ReportSectionTypeEnum.TextBlock => "fa-paragraph",
            ReportSectionTypeEnum.KPICards => "fa-tachometer-alt",
            _ => "fa-cube"
        };

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the column configuration as a strongly-typed object.
        /// </summary>
        public ReportColumnConfiguration GetColumnConfiguration()
        {
            if (string.IsNullOrEmpty(ColumnConfiguration))
                return new ReportColumnConfiguration();

            try
            {
                return JsonSerializer.Deserialize<ReportColumnConfiguration>(ColumnConfiguration, GetJsonOptions())
                    ?? new ReportColumnConfiguration();
            }
            catch
            {
                return new ReportColumnConfiguration();
            }
        }

        /// <summary>
        /// Sets the column configuration from a strongly-typed object.
        /// </summary>
        public void SetColumnConfiguration(ReportColumnConfiguration config)
        {
            ColumnConfiguration = JsonSerializer.Serialize(config, GetJsonOptions());
        }

        private static JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false
            };
        }

        #endregion
    }

    /// <summary>
    /// Enum representing report section types.
    /// </summary>
    public enum ReportSectionTypeEnum
    {
        /// <summary>AI-generated executive summary text.</summary>
        ExecutiveSummary = 0,

        /// <summary>Data table with sorting, filtering, and pagination.</summary>
        DataTable = 1,

        /// <summary>Chart visualization.</summary>
        Chart = 2,

        /// <summary>Static text block.</summary>
        TextBlock = 3,

        /// <summary>KPI cards row.</summary>
        KPICards = 4
    }

    /// <summary>
    /// Configuration for report columns including visibility, formatting, and conditional rules.
    /// </summary>
    public class ReportColumnConfiguration
    {
        /// <summary>
        /// Gets or sets the list of column definitions.
        /// </summary>
        public List<ReportColumnDefinition> Columns { get; set; } = new List<ReportColumnDefinition>();

        /// <summary>
        /// Gets or sets the default sort column.
        /// </summary>
        public string DefaultSortColumn { get; set; }

        /// <summary>
        /// Gets or sets the default sort direction (asc/desc).
        /// </summary>
        public string DefaultSortDirection { get; set; } = "asc";

        /// <summary>
        /// Gets or sets the default page size.
        /// </summary>
        public int DefaultPageSize { get; set; } = 25;

        /// <summary>
        /// Gets or sets whether to show row numbers.
        /// </summary>
        public bool ShowRowNumbers { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to enable row selection.
        /// </summary>
        public bool EnableRowSelection { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to show the search box.
        /// </summary>
        public bool ShowSearch { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to show column filters.
        /// </summary>
        public bool ShowColumnFilters { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to show pagination.
        /// </summary>
        public bool ShowPagination { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to show export options.
        /// </summary>
        public bool ShowExport { get; set; } = true;
    }

    /// <summary>
    /// Definition for a single column in a report section.
    /// </summary>
    public class ReportColumnDefinition
    {
        /// <summary>
        /// Gets or sets the column name (from SQL query).
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// Gets or sets the display header text.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets whether this column is visible.
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets the column width (px, %, or auto).
        /// </summary>
        public string Width { get; set; } = "auto";

        /// <summary>
        /// Gets or sets the data type for formatting.
        /// </summary>
        public ColumnDataTypeEnum DataType { get; set; } = ColumnDataTypeEnum.Text;

        /// <summary>
        /// Gets or sets the text alignment.
        /// </summary>
        public ColumnAlignmentEnum Alignment { get; set; } = ColumnAlignmentEnum.Left;

        /// <summary>
        /// Gets or sets the format string (e.g., "C2" for currency, "P1" for percentage).
        /// </summary>
        public string FormatString { get; set; }

        /// <summary>
        /// Gets or sets whether this column is sortable.
        /// </summary>
        public bool IsSortable { get; set; } = true;

        /// <summary>
        /// Gets or sets whether this column is filterable.
        /// </summary>
        public bool IsFilterable { get; set; } = true;

        /// <summary>
        /// Gets or sets the conditional formatting rules for this column.
        /// </summary>
        public List<ConditionalFormatRuleReport> ConditionalFormats { get; set; } = new List<ConditionalFormatRuleReport>();
    }

    /// <summary>
    /// Conditional formatting rule for a column.
    /// </summary>
    public class ConditionalFormatRuleReport
    {
        /// <summary>
        /// Gets or sets the condition operator.
        /// </summary>
        public ConditionalOperatorEnum Operator { get; set; }

        /// <summary>
        /// Gets or sets the comparison value.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the secondary value (for between operator).
        /// </summary>
        public string Value2 { get; set; }

        /// <summary>
        /// Gets or sets the background color when condition is met.
        /// </summary>
        public string BackgroundColor { get; set; }

        /// <summary>
        /// Gets or sets the text color when condition is met.
        /// </summary>
        public string TextColor { get; set; }

        /// <summary>
        /// Gets or sets the icon to display when condition is met.
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets whether to show as a badge.
        /// </summary>
        public bool ShowAsBadge { get; set; } = false;
    }

    /// <summary>
    /// Enum for column data types.
    /// </summary>
    public enum ColumnDataTypeEnum
    {
        /// <summary>Plain text.</summary>
        Text = 0,

        /// <summary>Numeric value.</summary>
        Number = 1,

        /// <summary>Currency value.</summary>
        Currency = 2,

        /// <summary>Percentage value.</summary>
        Percentage = 3,

        /// <summary>Date value.</summary>
        Date = 4,

        /// <summary>DateTime value.</summary>
        DateTime = 5,

        /// <summary>Boolean value.</summary>
        Boolean = 6
    }

    /// <summary>
    /// Enum for column alignment.
    /// </summary>
    public enum ColumnAlignmentEnum
    {
        /// <summary>Left aligned.</summary>
        Left = 0,

        /// <summary>Center aligned.</summary>
        Center = 1,

        /// <summary>Right aligned.</summary>
        Right = 2
    }

    /// <summary>
    /// Enum for conditional formatting operators.
    /// </summary>
    public enum ConditionalOperatorEnum
    {
        /// <summary>Equal to value.</summary>
        Equals = 0,

        /// <summary>Not equal to value.</summary>
        NotEquals = 1,

        /// <summary>Greater than value.</summary>
        GreaterThan = 2,

        /// <summary>Greater than or equal to value.</summary>
        GreaterThanOrEqual = 3,

        /// <summary>Less than value.</summary>
        LessThan = 4,

        /// <summary>Less than or equal to value.</summary>
        LessThanOrEqual = 5,

        /// <summary>Between two values.</summary>
        Between = 6,

        /// <summary>Contains text.</summary>
        Contains = 7,

        /// <summary>Starts with text.</summary>
        StartsWith = 8,

        /// <summary>Ends with text.</summary>
        EndsWith = 9,

        /// <summary>Is null or empty.</summary>
        IsEmpty = 10,

        /// <summary>Is not null or empty.</summary>
        IsNotEmpty = 11
    }
}
