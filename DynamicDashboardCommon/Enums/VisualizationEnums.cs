using System.ComponentModel;

namespace DynamicDashboardCommon.Enums
{
    /// <summary>
    /// Enum representing chart types for visualization components.
    /// </summary>
    public enum ChartTypeEnum
    {
        [Description("Bar Chart")]
        Bar = 1,

        [Description("Line Chart")]
        Line = 2,

        [Description("Pie Chart")]
        Pie = 3,

        [Description("Doughnut Chart")]
        Doughnut = 4,

        [Description("Area Chart")]
        Area = 5,

        [Description("Scatter Plot")]
        Scatter = 6,

        [Description("Radar Chart")]
        Radar = 7,

        [Description("Gauge Chart")]
        Gauge = 8,

        [Description("Funnel Chart")]
        Funnel = 9,

        [Description("Treemap")]
        Treemap = 10,

        [Description("Horizontal Bar Chart")]
        HorizontalBar = 11,

        [Description("Stacked Bar Chart")]
        StackedBar = 12,

        [Description("Combo Chart")]
        Combo = 13
    }

    /// <summary>
    /// Enum representing number format types for KPI/Number components.
    /// </summary>
    public enum NumberFormatEnum
    {
        [Description("Default (1,234.56)")]
        Default = 1,

        [Description("Currency ($1,234.56)")]
        Currency = 2,

        [Description("Percentage (12.34%)")]
        Percent = 3,

        [Description("Compact (1.2K, 3.4M)")]
        Compact = 4,

        [Description("Scientific (1.23E+4)")]
        Scientific = 5,

        [Description("Integer (1,234)")]
        Integer = 6
    }

    /// <summary>
    /// Enum representing text alignment options.
    /// </summary>
    public enum TextAlignEnum
    {
        [Description("Left")]
        Left = 1,

        [Description("Center")]
        Center = 2,

        [Description("Right")]
        Right = 3,

        [Description("Justify")]
        Justify = 4
    }

    /// <summary>
    /// Enum representing font size options.
    /// </summary>
    public enum FontSizeEnum
    {
        [Description("Small (12px)")]
        Small = 1,

        [Description("Medium (14px)")]
        Medium = 2,

        [Description("Large (18px)")]
        Large = 3,

        [Description("Extra Large (24px)")]
        XLarge = 4,

        [Description("Extra Extra Large (32px)")]
        XXLarge = 5
    }

    /// <summary>
    /// Enum representing chart legend position options.
    /// </summary>
    public enum LegendPositionEnum
    {
        [Description("Top")]
        Top = 1,

        [Description("Bottom")]
        Bottom = 2,

        [Description("Left")]
        Left = 3,

        [Description("Right")]
        Right = 4,

        [Description("Hidden")]
        None = 5
    }

    /// <summary>
    /// Enum representing component template categories.
    /// </summary>
    public enum ComponentCategoryEnum
    {
        [Description("Data Display")]
        DataDisplay = 1,

        [Description("Charts")]
        Charts = 2,

        [Description("KPIs")]
        KPIs = 3,

        [Description("Text")]
        Text = 4,

        [Description("Filters")]
        Filters = 5,

        [Description("Maps")]
        Maps = 6,

        [Description("Media")]
        Media = 7
    }

    /// <summary>
    /// Enum representing trend comparison types for KPI components.
    /// </summary>
    public enum TrendComparisonEnum
    {
        [Description("Previous Period")]
        PreviousPeriod = 1,

        [Description("Previous Year")]
        PreviousYear = 2,

        [Description("Target Value")]
        Target = 3,

        [Description("Moving Average")]
        MovingAverage = 4,

        [Description("None")]
        None = 5
    }

    /// <summary>
    /// Enum representing component interaction types.
    /// </summary>
    public enum InteractionTypeEnum
    {
        [Description("Cross Filter")]
        CrossFilter = 1,

        [Description("Drill Down")]
        Drilldown = 2,

        [Description("Parameter Link")]
        ParameterLink = 3,

        [Description("Navigation")]
        Navigation = 4
    }

    /// <summary>
    /// Enum representing table density options.
    /// </summary>
    public enum TableDensityEnum
    {
        [Description("Compact")]
        Compact = 1,

        [Description("Standard")]
        Standard = 2,

        [Description("Comfortable")]
        Comfortable = 3
    }

    /// <summary>
    /// Enum representing component cache strategies.
    /// </summary>
    public enum CacheStrategyEnum
    {
        [Description("No Cache")]
        None = 1,

        [Description("Session Cache")]
        Session = 2,

        [Description("Timed Cache")]
        Timed = 3,

        [Description("Dashboard Refresh Only")]
        DashboardRefresh = 4
    }
}