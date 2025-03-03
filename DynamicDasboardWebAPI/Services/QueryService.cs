using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DynamicDasboardWebAPI.Repositories;
using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Utilities;
using Microsoft.Extensions.Logging;

namespace DynamicDasboardWebAPI.Services
{
    public class QueryService
    {
        private readonly QueryRepository _repository;
        private readonly QueryLogsRepository _logsRepository;
        private readonly ILogger<QueryService> _logger;

        public QueryService(
            QueryRepository repository,
            QueryLogsRepository logsRepository,
            ILogger<QueryService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logsRepository = logsRepository ?? throw new ArgumentNullException(nameof(logsRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<DirectSqlResult> ExecuteDirectQueryAsync(DirectSqlRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.SqlQuery))
                throw new ArgumentException("Query cannot be null or empty.");

            if (request.DatabaseId <= 0)
                throw new ArgumentException("Database ID must be specified.");

            try
            {
                _logger.LogInformation($"Executing query on database {request.DatabaseId}: {request.SqlQuery}");

                // Execute query through repository
                var data = await _repository.ExecuteQueryAsync(request.SqlQuery, request.DatabaseId);

                // Log the successful query
                await _logsRepository.LogQueryAsync(request.SqlQuery, request.UserId, request.DatabaseId, "Success");

                // Return successful result
                return new DirectSqlResult
                {
                    Data = data,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing query: {ex.Message}");

                // Log the error
                await _logsRepository.LogQueryAsync(request.SqlQuery, request.UserId, request.DatabaseId, $"Error: {ex.Message}");

                // Return error result
                return new DirectSqlResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string query, int databaseId, int? executedBy)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be null or empty.");

            if (databaseId <= 0)
                throw new ArgumentException("Database ID must be specified.");

            try
            {
                var result = await _repository.ExecuteQueryAsync(query, databaseId);
                await _logsRepository.LogQueryAsync(query, executedBy, databaseId, "Success");
                return result;
            }
            catch (Exception ex)
            {
                await _logsRepository.LogQueryAsync(query, executedBy, databaseId, $"Error: {ex.Message}");
                throw;
            }
        }
    }
}