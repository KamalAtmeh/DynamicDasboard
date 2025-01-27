using System;

namespace DynamicDashboardCommon.Models
{
    /// <summary>
    /// The User class contains properties that define a user, including their ID, username, role, allowed databases, password hash, and creation date.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// </summary>
        public int UserID { get; set; }

        /// <summary>
        /// Gets or sets the username of the user.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the role ID associated with the user.
        /// </summary>
        public int RoleID { get; set; }

        /// <summary>
        /// Gets or sets the databases that the user is allowed to access.
        /// </summary>
        public string AllowedDatabases { get; set; }

        /// <summary>
        /// Gets or sets the hashed password of the user.
        /// </summary>
        public string PasswordHash { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the user was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}