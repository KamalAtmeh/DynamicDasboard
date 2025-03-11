using System;
using System.Threading.Tasks;
using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Repositories;
using Microsoft.Extensions.Logging;

namespace DynamicDasboardWebAPI.Services
{
    public class DatabaseSchemaService
    {
        private readonly DatabaseJsonSchemaRepository _repository;

        public DatabaseSchemaService(DatabaseJsonSchemaRepository repository)
        {
            _repository = repository;
            
        }

        public async Task<int> CreateSchemaAsync(DatabaseSchema schema)
        {
            try
            {
                return await _repository.InsertDatabaseJsonSchemaAsync(schema);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<int> UpdateSchemaAsync(DatabaseSchema schema)
        {
            try
            {
                return await _repository.UpdateDatabaseJsonSchemaAsync(schema);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<DatabaseSchema> GetSchemaByIdAsync(int id)
        {
            try
            {
                return await _repository.GetDatabaseJsonSchemaByIdAsync(id);
            }
            catch (Exception ex)
            {
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
                throw;
            }
        }
    }
}
