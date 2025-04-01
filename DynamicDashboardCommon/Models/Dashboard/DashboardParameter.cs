using System;

namespace DynamicDashboardCommon.Models
{
    /// <summary>
    /// Represents a parameter for a dashboard component query.
    /// </summary>
    public class ComponentParameter
    {
        /// <summary>
        /// Gets or sets the unique identifier for the parameter.
        /// </summary>
        public int ParameterID { get; set; }

        /// <summary>
        /// Gets or sets the component ID this parameter belongs to.
        /// </summary>
        public int ComponentID { get; set; }

        /// <summary>
        /// Gets or sets the name of the parameter.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the display name of the parameter.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the default value of the parameter.
        /// </summary>
        public string DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets the current value of the parameter.
        /// </summary>
        public string CurrentValue { get; set; }

        /// <summary>
        /// Gets or sets the data type of the parameter.
        /// </summary>
        public ParameterDataType DataType { get; set; }

        /// <summary>
        /// Gets or sets whether this parameter is required.
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Gets or sets the parameter options for selection (for dropdown or multi-select parameters).
        /// </summary>
        public string Options { get; set; }

        /// <summary>
        /// Gets or sets whether this parameter is visible to users.
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Gets or sets validation rules for the parameter (JSON format).
        /// </summary>
        public string ValidationRules { get; set; }

        /// <summary>
        /// Gets or sets the description of the parameter.
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// Enum representing parameter data types.
    /// </summary>
    public enum ParameterDataType
    {
        String = 1,
        Number = 2,
        Date = 3,
        Boolean = 4,
        Array = 5,
        DateTime = 6
    }
}