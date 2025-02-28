using System;

namespace DynamicDashboardCommon.Models
{
    /// <summary>
    /// Represents the different ways data can be viewed in the system
    /// </summary>
    public class DataViewingType
    {
        /// <summary>
        /// Unique identifier for the data viewing type
        /// </summary>
        public int DataViewingTypeID { get; set; }

        /// <summary>
        /// Name of the viewing type (e.g., Table, Label, Number, Chart)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Optional description of the viewing type
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// Enum representing predefined data viewing types
    /// </summary>
    public enum DataViewingTypeEnum
    {
        Table = 1,
        Label = 2,
        Number = 3,
        Chart = 4,
        Card = 5
    }
}