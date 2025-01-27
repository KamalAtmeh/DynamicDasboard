using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicDashboardCommon.Models
{
    /// <summary>
    /// Represents metadata information for a database table.
    /// </summary>
    public class TableMetadata
    {
        /// <summary>
        /// Gets or sets the name of the database table.
        /// </summary>
        public string DBTableName { get; set; }

        /// <summary>
        /// Gets or sets the name of the admin table.
        /// </summary>
        public string AdminTableName { get; set; }

        /// <summary>
        /// Gets or sets the name of the database column.
        /// </summary>
        public string DBColumnName { get; set; }

        /// <summary>
        /// Gets or sets the name of the admin column.
        /// </summary>
        public string AdmincnName { get; set; }

        /// <summary>
        /// Gets or sets the data type of the column.
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the column is nullable.
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the column is a primary key.
        /// </summary>
        public bool IsPrimary { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the column is a foreign key.
        /// </summary>
        public bool IsFK { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the column is unique.
        /// </summary>
        public bool IsUnique { get; set; }

        /// <summary>
        /// Gets or sets the description of the database column.
        /// </summary>
        public string DBDescription { get; set; }
    }
}
