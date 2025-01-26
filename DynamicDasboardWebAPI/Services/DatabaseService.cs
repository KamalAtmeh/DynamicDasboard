using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly DatabaseRepository _repository;

        public DatabaseService(DatabaseRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Database>> GetAllDatabasesAsync()
        {
            return await _repository.GetAllDatabasesAsync();
        }

        public async Task<int> AddDatabaseAsync(Database database)
        {
            if (string.IsNullOrWhiteSpace(database.Name))
                throw new ArgumentException("Database name cannot be empty.");

            if (string.IsNullOrWhiteSpace(database.ConnectionString))
                throw new ArgumentException("Connection string cannot be empty.");

            if (!await _repository.TestConnectionAsync(database))
                throw new ArgumentException("Invalid connection string.");

            return await _repository.AddDatabaseAsync(database);
        }

        public async Task<int> UpdateDatabaseAsync(Database database)
        {
            if (string.IsNullOrWhiteSpace(database.Name))
                throw new ArgumentException("Database name cannot be empty.");

            if (string.IsNullOrWhiteSpace(database.ConnectionString))
                throw new ArgumentException("Connection string cannot be empty.");

            if (!await _repository.TestConnectionAsync(database))
                throw new ArgumentException("Invalid connection string.");

            return await _repository.UpdateDatabaseAsync(database);
        }

        public async Task<int> DeleteDatabaseAsync(int databaseId)
        {
            return await _repository.DeleteDatabaseAsync(databaseId);
        }

        public async Task<bool> TestConnectionAsync(Database database)
        {
            return await _repository.TestConnectionAsync(database);
        }

        public bool GetDatabaseMetadataAsync(int databaseID)
        {
            return false;
        }
    }
}