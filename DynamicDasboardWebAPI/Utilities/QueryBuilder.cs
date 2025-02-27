namespace DynamicDasboardWebAPI.Utilities
{
    /// <summary>
    /// The QueryBuilder class is responsible for constructing SQL queries dynamically.
    /// This class provides methods to build SELECT, INSERT, UPDATE, and DELETE queries
    /// based on the provided parameters. It helps in creating complex queries in a 
    /// programmatic way, ensuring that the queries are syntactically correct and 
    /// preventing SQL injection attacks by using parameterized queries.
    /// </summary>
    public static class QueryBuilder
    {
        // Future methods for building SQL queries will be added here.


        /// <summary>
        /// Extracts the SQL query from a markdown code block
        /// </summary>
        /// <param name="input">Input string containing a SQL query with markdown code block syntax</param>
        /// <returns>The clean SQL query without markdown delimiters</returns>
        public static string ExtractSqlQueryFromMarkdown(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Remove the opening markdown code block identifier
            string cleanQuery = input.Replace("```sql", "").Replace("```", "");

            // Trim any leading/trailing whitespace
            cleanQuery = cleanQuery.Trim();

            return cleanQuery;
        }
    }
}
