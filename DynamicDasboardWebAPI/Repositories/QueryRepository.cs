using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using DynamicDasboardWebAPI.Utilities;

namespace DynamicDasboardWebAPI.Repositories
{
    public class QueryRepository
    {
        private readonly DbConnectionFactory _dbConnectionFactory;

        public QueryRepository(DbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory ?? throw new ArgumentNullException(nameof(dbConnectionFactory));
        }

        public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string query, string dbType)
        {
            var result = new List<Dictionary<string, object>>();

            using (var connection = _dbConnectionFactory.CreateConnection(dbType))
            {
                await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>();
                            for (var i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.GetValue(i);
                            }
                            result.Add(row);
                        }
                    }
                }
            }

            return result;
        }
    }
}