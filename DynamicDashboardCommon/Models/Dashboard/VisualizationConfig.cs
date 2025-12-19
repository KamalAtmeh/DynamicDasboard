using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using DynamicDashboardCommon.Enums;

namespace DynamicDashboardCommon.Models
{
    /// <summary>
    /// Represents the visualization configuration for a dashboard component.
    /// This model is serialized to JSON and stored in the VisualizationConfig column.
    /// </summary>
    public class VisualizationConfig
    {
        #region Common Properties

        /// <summary>
        /// Gets or sets the background color of the component.
        /// </summary>
        public string BackgroundColor { get; set; } = "#ffffff";

        /// <summary>
        /// Gets or sets the border radius in pixels.
        /// </summary>
        public int BorderRadius { get; set; } = 8;

        /// <summary>
        /// Gets or sets the padding in pixels.
        /// </summary>
        public int Padding { get; set; } = 16;

        /// <summary>
        /// Gets or sets whether to show shadow.
        /// </summary>
        public bool ShowShadow { get; set; } = true;

        /// <summary>
        /// Gets or sets the primary color for the component.
        /// </summary>
        public string PrimaryColor { get; set; } = "#667eea";

        /// <summary>
        /// Gets or sets the secondary color for the component.
        /// </summary>
        public string SecondaryColor { get; set; } = "#764ba2";

        /// <summary>
        /// Gets or sets the color scheme key.
        /// </summary>
        public string ColorScheme { get; set; } = "default";

        /// <summary>
        /// Gets or sets the border color.
        /// </summary>
        public string BorderColor { get; set; }

        /// <summary>
        /// Gets or sets the border width in pixels.
        /// </summary>
        public int BorderWidth { get; set; } = 0;

        /// <summary>
        /// Gets or sets custom CSS class names.
        /// </summary>
        public string CustomCssClass { get; set; }

        #endregion

        #region Chart Properties

        /// <summary>
        /// Gets or sets the chart type.
        /// </summary>
        public ChartTypeEnum ChartType { get; set; } = ChartTypeEnum.Bar;

        /// <summary>
        /// Gets or sets the chart type as string for JSON serialization compatibility.
        /// </summary>
        [JsonPropertyName("chartTypeString")]
        public string ChartTypeString
        {
            get => ChartType.ToString().ToLower();
            set
            {
                if (Enum.TryParse<ChartTypeEnum>(value, true, out var result))
                {
                    ChartType = result;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to show the chart legend.
        /// </summary>
        public bool ShowLegend { get; set; } = true;

        /// <summary>
        /// Gets or sets the legend position.
        /// </summary>
        public LegendPositionEnum LegendPosition { get; set; } = LegendPositionEnum.Top;

        /// <summary>
        /// Gets or sets whether to show data labels on the chart.
        /// </summary>
        public bool ShowDataLabels { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to show grid lines.
        /// </summary>
        public bool ShowGridLines { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable chart animations.
        /// </summary>
        public bool EnableAnimation { get; set; } = true;

        /// <summary>
        /// Gets or sets the animation duration in milliseconds.
        /// </summary>
        public int AnimationDuration { get; set; } = 750;

        /// <summary>
        /// Gets or sets the X-axis label.
        /// </summary>
        public string XAxisLabel { get; set; }

        /// <summary>
        /// Gets or sets the Y-axis label.
        /// </summary>
        public string YAxisLabel { get; set; }

        /// <summary>
        /// Gets or sets the data field for X-axis.
        /// </summary>
        public string XAxisField { get; set; }

        /// <summary>
        /// Gets or sets the data field for Y-axis.
        /// </summary>
        public string YAxisField { get; set; }

        /// <summary>
        /// Gets or sets the field used for color grouping.
        /// </summary>
        public string ColorByField { get; set; }

        /// <summary>
        /// Gets or sets whether to stack series (for bar/area charts).
        /// </summary>
        public bool IsStacked { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to show tooltips on hover.
        /// </summary>
        public bool ShowTooltip { get; set; } = true;

        /// <summary>
        /// Gets or sets the minimum Y-axis value (null for auto).
        /// </summary>
        public decimal? YAxisMin { get; set; }

        /// <summary>
        /// Gets or sets the maximum Y-axis value (null for auto).
        /// </summary>
        public decimal? YAxisMax { get; set; }

        /// <summary>
        /// Gets or sets whether the chart is horizontal.
        /// </summary>
        public bool IsHorizontal { get; set; } = false;

        /// <summary>
        /// Gets or sets the inner radius for doughnut charts (0-1).
        /// </summary>
        public decimal DoughnutInnerRadius { get; set; } = 0.5m;

        /// <summary>
        /// Gets or sets the series configuration for multi-series charts.
        /// </summary>
        public List<ChartSeriesConfig> Series { get; set; } = new List<ChartSeriesConfig>();

        #endregion

        #region Table Properties

        /// <summary>
        /// Gets or sets whether to show the table header.
        /// </summary>
        public bool ShowHeader { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to use striped rows.
        /// </summary>
        public bool StripedRows { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable row hover effect.
        /// </summary>
        public bool HoverEffect { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable sorting.
        /// </summary>
        public bool EnableSorting { get; set; } = true;

        /// <summary>
        /// Gets or sets the default sort column.
        /// </summary>
        public string DefaultSortColumn { get; set; }

        /// <summary>
        /// Gets or sets the default sort direction (asc/desc).
        /// </summary>
        public string DefaultSortDirection { get; set; } = "asc";

        /// <summary>
        /// Gets or sets whether to enable pagination.
        /// </summary>
        public bool EnablePagination { get; set; } = true;

        /// <summary>
        /// Gets or sets the page size for pagination.
        /// </summary>
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// Gets or sets available page size options.
        /// </summary>
        public List<int> PageSizeOptions { get; set; } = new List<int> { 5, 10, 25, 50, 100 };

        /// <summary>
        /// Gets or sets whether to enable column resizing.
        /// </summary>
        public bool EnableColumnResize { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to enable column reordering.
        /// </summary>
        public bool EnableColumnReorder { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to enable row selection.
        /// </summary>
        public bool EnableRowSelection { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to allow multiple row selection.
        /// </summary>
        public bool AllowMultiSelect { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to show row numbers.
        /// </summary>
        public bool ShowRowNumbers { get; set; } = false;

        /// <summary>
        /// Gets or sets the table density.
        /// </summary>
        public TableDensityEnum TableDensity { get; set; } = TableDensityEnum.Standard;

        /// <summary>
        /// Gets or sets whether to enable search/filtering.
        /// </summary>
        public bool EnableSearch { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to show column filters.
        /// </summary>
        public bool ShowColumnFilters { get; set; } = false;

        /// <summary>
        /// Gets or sets whether table has fixed header when scrolling.
        /// </summary>
        public bool FixedHeader { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum table height in pixels (null for auto).
        /// </summary>
        public int? MaxHeight { get; set; }

        /// <summary>
        /// Gets or sets the column configurations.
        /// </summary>
        public List<TableColumnConfig> Columns { get; set; } = new List<TableColumnConfig>();

        /// <summary>
        /// Gets or sets conditional formatting rules for the table.
        /// </summary>
        public List<ConditionalFormatRule> ConditionalFormats { get; set; } = new List<ConditionalFormatRule>();

        #endregion

        #region Card/KPI Properties

        /// <summary>
        /// Gets or sets the icon for the card (FontAwesome class without fa- prefix).
        /// </summary>
        public string Icon { get; set; } = "chart-line";

        /// <summary>
        /// Gets or sets the icon color.
        /// </summary>
        public string IconColor { get; set; }

        /// <summary>
        /// Gets or sets the icon background color.
        /// </summary>
        public string IconBackgroundColor { get; set; }

        /// <summary>
        /// Gets or sets the number format.
        /// </summary>
        public NumberFormatEnum NumberFormat { get; set; } = NumberFormatEnum.Default;

        /// <summary>
        /// Gets or sets the currency symbol for currency format.
        /// </summary>
        public string CurrencySymbol { get; set; } = "$";

        /// <summary>
        /// Gets or sets the currency code (USD, EUR, etc.).
        /// </summary>
        public string CurrencyCode { get; set; } = "USD";

        /// <summary>
        /// Gets or sets the number of decimal places.
        /// </summary>
        public int DecimalPlaces { get; set; } = 2;

        /// <summary>
        /// Gets or sets the thousands separator.
        /// </summary>
        public string ThousandsSeparator { get; set; } = ",";

        /// <summary>
        /// Gets or sets the decimal separator.
        /// </summary>
        public string DecimalSeparator { get; set; } = ".";

        /// <summary>
        /// Gets or sets whether to show trend indicator.
        /// </summary>
        public bool ShowTrend { get; set; } = true;

        /// <summary>
        /// Gets or sets the trend comparison type.
        /// </summary>
        public TrendComparisonEnum TrendComparison { get; set; } = TrendComparisonEnum.PreviousPeriod;

        /// <summary>
        /// Gets or sets the target value for comparison.
        /// </summary>
        public decimal? TargetValue { get; set; }

        /// <summary>
        /// Gets or sets whether positive trend is good (affects color).
        /// </summary>
        public bool PositiveIsGood { get; set; } = true;

        /// <summary>
        /// Gets or sets the value field name from query results.
        /// </summary>
        public string ValueField { get; set; }

        /// <summary>
        /// Gets or sets the comparison value field name.
        /// </summary>
        public string ComparisonField { get; set; }

        /// <summary>
        /// Gets or sets the label field name.
        /// </summary>
        public string LabelField { get; set; }

        /// <summary>
        /// Gets or sets prefix text before the value.
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// Gets or sets suffix text after the value.
        /// </summary>
        public string Suffix { get; set; }

        /// <summary>
        /// Gets or sets whether to show sparkline.
        /// </summary>
        public bool ShowSparkline { get; set; } = false;

        /// <summary>
        /// Gets or sets the sparkline data field.
        /// </summary>
        public string SparklineField { get; set; }

        /// <summary>
        /// Gets or sets threshold values for color coding.
        /// </summary>
        public List<ThresholdConfig> Thresholds { get; set; } = new List<ThresholdConfig>();

        #endregion

        #region Label Properties

        /// <summary>
        /// Gets or sets the text content for label components.
        /// </summary>
        public string TextContent { get; set; }

        /// <summary>
        /// Gets or sets the font size.
        /// </summary>
        public FontSizeEnum FontSize { get; set; } = FontSizeEnum.Medium;

        /// <summary>
        /// Gets or sets custom font size in pixels (overrides FontSize enum if set).
        /// </summary>
        public int? CustomFontSize { get; set; }

        /// <summary>
        /// Gets or sets the font family.
        /// </summary>
        public string FontFamily { get; set; }

        /// <summary>
        /// Gets or sets the text alignment.
        /// </summary>
        public TextAlignEnum TextAlign { get; set; } = TextAlignEnum.Left;

        /// <summary>
        /// Gets or sets the text color.
        /// </summary>
        public string TextColor { get; set; } = "#2d3748";

        /// <summary>
        /// Gets or sets whether the text is bold.
        /// </summary>
        public bool IsBold { get; set; } = false;

        /// <summary>
        /// Gets or sets whether the text is italic.
        /// </summary>
        public bool IsItalic { get; set; } = false;

        /// <summary>
        /// Gets or sets whether the text is underlined.
        /// </summary>
        public bool IsUnderline { get; set; } = false;

        /// <summary>
        /// Gets or sets the line height multiplier.
        /// </summary>
        public decimal LineHeight { get; set; } = 1.5m;

        /// <summary>
        /// Gets or sets whether to enable markdown rendering.
        /// </summary>
        public bool EnableMarkdown { get; set; } = false;

        #endregion

        #region Interaction Properties

        /// <summary>
        /// Gets or sets whether drill-down is enabled.
        /// </summary>
        public bool EnableDrilldown { get; set; } = false;

        /// <summary>
        /// Gets or sets the drill-down target component ID.
        /// </summary>
        public int? DrilldownTargetId { get; set; }

        /// <summary>
        /// Gets or sets the drill-down URL (for external navigation).
        /// </summary>
        public string DrilldownUrl { get; set; }

        /// <summary>
        /// Gets or sets the drill-down parameter name.
        /// </summary>
        public string DrilldownParameter { get; set; }

        /// <summary>
        /// Gets or sets whether cross-filtering is enabled.
        /// </summary>
        public bool EnableCrossFilter { get; set; } = false;

        /// <summary>
        /// Gets or sets the cross-filter target component IDs.
        /// </summary>
        public List<int> CrossFilterTargets { get; set; } = new List<int>();

        /// <summary>
        /// Gets or sets the field to use for cross-filtering.
        /// </summary>
        public string CrossFilterField { get; set; }

        /// <summary>
        /// Gets or sets whether data export is enabled.
        /// </summary>
        public bool EnableExport { get; set; } = true;

        /// <summary>
        /// Gets or sets the allowed export formats.
        /// </summary>
        public List<string> ExportFormats { get; set; } = new List<string> { "csv", "excel", "pdf" };

        /// <summary>
        /// Gets or sets whether to show fullscreen button.
        /// </summary>
        public bool ShowFullscreenButton { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to show refresh button.
        /// </summary>
        public bool ShowRefreshButton { get; set; } = true;

        #endregion

        #region Serialization Methods

        /// <summary>
        /// Serializes the configuration to JSON string.
        /// </summary>
        /// <returns>JSON string representation.</returns>
        public string ToJson()
        {
            return JsonSerializer.Serialize(this, GetJsonOptions());
        }

        /// <summary>
        /// Deserializes a JSON string to VisualizationConfig.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <returns>VisualizationConfig instance.</returns>
        public static VisualizationConfig FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new VisualizationConfig();

            try
            {
                return JsonSerializer.Deserialize<VisualizationConfig>(json, GetJsonOptions())
                    ?? new VisualizationConfig();
            }
            catch
            {
                return new VisualizationConfig();
            }
        }

        /// <summary>
        /// Gets the JSON serializer options.
        /// </summary>
        private static JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            };
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates a default configuration for a chart component.
        /// </summary>
        public static VisualizationConfig CreateChartDefault(ChartTypeEnum chartType = ChartTypeEnum.Bar)
        {
            return new VisualizationConfig
            {
                ChartType = chartType,
                ShowLegend = true,
                ShowGridLines = true,
                EnableAnimation = true,
                ShowTooltip = true,
                ColorScheme = "default"
            };
        }

        /// <summary>
        /// Creates a default configuration for a table component.
        /// </summary>
        public static VisualizationConfig CreateTableDefault()
        {
            return new VisualizationConfig
            {
                ShowHeader = true,
                StripedRows = true,
                HoverEffect = true,
                EnableSorting = true,
                EnablePagination = true,
                PageSize = 10,
                TableDensity = TableDensityEnum.Standard,
                FixedHeader = true
            };
        }

        /// <summary>
        /// Creates a default configuration for a KPI/Number component.
        /// </summary>
        public static VisualizationConfig CreateKpiDefault()
        {
            return new VisualizationConfig
            {
                Icon = "chart-line",
                NumberFormat = NumberFormatEnum.Default,
                ShowTrend = true,
                TrendComparison = TrendComparisonEnum.PreviousPeriod,
                PositiveIsGood = true,
                DecimalPlaces = 2
            };
        }

        /// <summary>
        /// Creates a default configuration for a label component.
        /// </summary>
        public static VisualizationConfig CreateLabelDefault()
        {
            return new VisualizationConfig
            {
                FontSize = FontSizeEnum.Medium,
                TextAlign = TextAlignEnum.Left,
                TextColor = "#2d3748",
                IsBold = false,
                LineHeight = 1.5m
            };
        }

        /// <summary>
        /// Creates a default configuration for a card component.
        /// </summary>
        public static VisualizationConfig CreateCardDefault()
        {
            return new VisualizationConfig
            {
                BackgroundColor = "#ffffff",
                BorderRadius = 8,
                Padding = 16,
                ShowShadow = true
            };
        }

        /// <summary>
        /// Creates default configuration based on data viewing type.
        /// </summary>
        public static VisualizationConfig CreateDefault(DataViewingTypeEnum dataViewingType)
        {
            return dataViewingType switch
            {
                DataViewingTypeEnum.Chart => CreateChartDefault(),
                DataViewingTypeEnum.Table => CreateTableDefault(),
                DataViewingTypeEnum.Number => CreateKpiDefault(),
                DataViewingTypeEnum.Label => CreateLabelDefault(),
                DataViewingTypeEnum.Card => CreateCardDefault(),
                _ => new VisualizationConfig()
            };
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Configuration for a chart series.
    /// </summary>
    public class ChartSeriesConfig
    {
        /// <summary>
        /// Gets or sets the series name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the data field for this series.
        /// </summary>
        public string DataField { get; set; }

        /// <summary>
        /// Gets or sets the series color.
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// Gets or sets the chart type for this series (for mixed charts).
        /// </summary>
        public ChartTypeEnum? ChartType { get; set; }

        /// <summary>
        /// Gets or sets whether this series uses the secondary Y-axis.
        /// </summary>
        public bool UseSecondaryAxis { get; set; } = false;

        /// <summary>
        /// Gets or sets the line style (solid, dashed, dotted).
        /// </summary>
        public string LineStyle { get; set; } = "solid";

        /// <summary>
        /// Gets or sets the point style for line charts.
        /// </summary>
        public string PointStyle { get; set; } = "circle";

        /// <summary>
        /// Gets or sets whether to fill area under line.
        /// </summary>
        public bool Fill { get; set; } = false;
    }

    /// <summary>
    /// Configuration for a table column.
    /// </summary>
    public class TableColumnConfig
    {
        /// <summary>
        /// Gets or sets the column field name.
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// Gets or sets the column header text.
        /// </summary>
        public string Header { get; set; }

        /// <summary>
        /// Gets or sets the column width (e.g., "100px", "20%", "auto").
        /// </summary>
        public string Width { get; set; } = "auto";

        /// <summary>
        /// Gets or sets the minimum column width.
        /// </summary>
        public string MinWidth { get; set; }

        /// <summary>
        /// Gets or sets the maximum column width.
        /// </summary>
        public string MaxWidth { get; set; }

        /// <summary>
        /// Gets or sets the column alignment.
        /// </summary>
        public TextAlignEnum Align { get; set; } = TextAlignEnum.Left;

        /// <summary>
        /// Gets or sets whether the column is sortable.
        /// </summary>
        public bool Sortable { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the column is visible.
        /// </summary>
        public bool Visible { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the column is frozen (fixed position).
        /// </summary>
        public bool Frozen { get; set; } = false;

        /// <summary>
        /// Gets or sets the format type (text, number, currency, percent, date, boolean).
        /// </summary>
        public string Format { get; set; } = "text";

        /// <summary>
        /// Gets or sets the format string for date/number formatting.
        /// </summary>
        public string FormatString { get; set; }

        /// <summary>
        /// Gets or sets the display order of the column.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Gets or sets whether to wrap text in this column.
        /// </summary>
        public bool WrapText { get; set; } = false;

        /// <summary>
        /// Gets or sets a custom template for rendering cell content.
        /// </summary>
        public string Template { get; set; }
    }

    /// <summary>
    /// Conditional formatting rule for tables.
    /// </summary>
    public class ConditionalFormatRule
    {
        /// <summary>
        /// Gets or sets the rule name/identifier.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the target field.
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// Gets or sets the condition type (equals, greaterThan, lessThan, between, contains).
        /// </summary>
        public string Condition { get; set; }

        /// <summary>
        /// Gets or sets the comparison value.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Gets or sets the secondary value for "between" conditions.
        /// </summary>
        public object Value2 { get; set; }

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
        /// Gets or sets whether the rule is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the priority (lower number = higher priority).
        /// </summary>
        public int Priority { get; set; } = 0;
    }

    /// <summary>
    /// Threshold configuration for KPI color coding.
    /// </summary>
    public class ThresholdConfig
    {
        /// <summary>
        /// Gets or sets the threshold value.
        /// </summary>
        public decimal Value { get; set; }

        /// <summary>
        /// Gets or sets the comparison operator (lt, lte, gt, gte, eq).
        /// </summary>
        public string Operator { get; set; } = "gte";

        /// <summary>
        /// Gets or sets the color when threshold is met.
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// Gets or sets the icon when threshold is met.
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets the label for this threshold.
        /// </summary>
        public string Label { get; set; }
    }

    ///// <summary>
    ///// Color scheme definition for visualizations.
    ///// </summary>
    //public class ColorScheme
    //{
    //    /// <summary>
    //    /// Gets or sets the scheme key/identifier.
    //    /// </summary>
    //    public string Key { get; set; }

    //    /// <summary>
    //    /// Gets or sets the display name.
    //    /// </summary>
    //    public string Name { get; set; }

    //    /// <summary>
    //    /// Gets or sets the list of colors in this scheme.
    //    /// </summary>
    //    public List<string> Colors { get; set; } = new List<string>();

    //    /// <summary>
    //    /// Gets or sets whether this is a dark theme scheme.
    //    /// </summary>
    //    public bool IsDarkTheme { get; set; }
    //}

    #endregion
}