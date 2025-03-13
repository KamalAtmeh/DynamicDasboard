using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicDashboardCommon.Models
{
    /// <summary>
    /// Represents a column in a database table.
    /// This class is used to define the properties of a column, including its ID, name, data type, and other attributes.
    /// </summary>
    public class Column
    {
        /// <summary>
        /// Gets or sets the unique identifier for the column.
        /// </summary>
        public int ColumnID { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the table to which this column belongs.
        /// </summary>
        public int TableID { get; set; } // Foreign key to the Table table

        /// <summary>
        /// Gets or sets the actual name of the column in the database.
        /// </summary>
        public string DBColumnName { get; set; } // Actual column name in the database

        /// <summary>
        /// Gets or sets the user-friendly name for the column.
        /// </summary>
        public string AdminColumnName { get; set; } // User-friendly name for the column

        /// <summary>
        /// Gets or sets the data type of the column (e.g., varchar, int).
        /// </summary>
        public string DataType { get; set; } // Data type of the column (e.g., varchar, int)

        /// <summary>
        /// Gets or sets a value indicating whether the column allows null values.
        /// </summary>
        public bool IsNullable { get; set; } // Whether the column allows null values

        /// <summary>
        /// Gets or sets the description provided by the admin for the column.
        /// </summary>
        public string AdminDescription { get; set; } // Description provided by the admin

        /// <summary>
        /// Gets or sets a value indicating whether this column is a lookup column
        /// that references data in another table or represents a key concept.
        /// </summary>
        public bool IsLookupColumn { get; set; }
    }
}
