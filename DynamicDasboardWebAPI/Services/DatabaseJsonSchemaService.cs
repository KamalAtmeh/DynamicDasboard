using System;
using System.Threading.Tasks;
using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Repositories;
using Microsoft.Extensions.Logging;

namespace DynamicDasboardWebAPI.Services
{
    public class DatabaseJsonSchemaService
    {
        private readonly DatabaseJsonSchemaRepository _repository;
        private readonly ILogger<DatabaseJsonSchemaService> _logger;

        public DatabaseJsonSchemaService(DatabaseJsonSchemaRepository repository, ILogger<DatabaseJsonSchemaService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<int> CreateSchemaAsync(DatabaseJsonSchema schema)
        {
            try
            {
                return await _repository.InsertDatabaseJsonSchemaAsync(schema);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting database JSON schema.");
                throw;
            }
        }

        public async Task<int> UpdateSchemaAsync(DatabaseJsonSchema schema)
        {
            try
            {
                return await _repository.UpdateDatabaseJsonSchemaAsync(schema);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating database JSON schema.");
                throw;
            }
        }

        public async Task<DatabaseJsonSchema> GetSchemaByIdAsync(int id)
        {
            try
            {
                return await _repository.GetDatabaseJsonSchemaByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving database JSON schema.");
                throw;
            }
        }

        public async Task<int> DeactivateSchemaAsync(int id)
        {
            try
            {
                return await _repository.DeactivateDatabaseJsonSchemaAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating database JSON schema.");
                throw;
            }
        }
    }
}
