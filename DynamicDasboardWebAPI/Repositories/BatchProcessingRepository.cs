using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Utilities;

namespace DynamicDasboardWebAPI.Repositories
{
    public class BatchProcessingRepository
    {
        private readonly IDbConnection _appDbConnection;
        private readonly DbConnectionFactory _connectionFactory;
        private readonly ILogger<DatabaseRepository> _logger;

        public BatchProcessingRepository(
                IDbConnection appDbConnection,
                DbConnectionFactory connectionFactory,
                ILogger<DatabaseRepository> logger = null)
        {
            _appDbConnection = appDbConnection ?? throw new ArgumentNullException(nameof(appDbConnection));
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger;
        }

        /// <summary>
        /// Logs a batch processing job.
        /// </summary>
        /// <param name="fileName">The name of the processed file.</param>
        /// <param name="totalQuestions">The total number of questions processed.</param>
        /// <param name="successCount">The number of successfully processed questions.</param>
        /// <param name="userId">The user ID (optional).</param>
        /// <returns>The ID of the inserted log.</returns>
        public async Task<int> LogBatchJobAsync(string fileName, int totalQuestions, int successCount, int? userId)
        {
            try
            {
                string query = @"
                    INSERT INTO BatchProcessingLogs (FileName, TotalQuestions, SuccessCount, UserId, ProcessedAt)
                    VALUES (@FileName, @TotalQuestions, @SuccessCount, @UserId, GETDATE());
                    SELECT SCOPE_IDENTITY();";

                var parameters = new
                {
                    FileName = fileName,
                    TotalQuestions = totalQuestions,
                    SuccessCount = successCount,
                    UserId = userId
                };

                return await _appDbConnection.ExecuteScalarSafeAsync<int>(query, parameters);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Logs details for each question in a batch.
        /// </summary>
        /// <param name="batchId">The ID of the batch processing log.</param>
        /// <param name="question">The natural language question.</param>
        /// <param name="generatedSql">The generated SQL query.</param>
        /// <param name="success">Whether the processing was successful.</param>
        /// <param name="errorMessage">Error message if processing failed (optional).</param>
        /// <returns>The ID of the inserted log detail.</returns>
        public async Task<int> LogBatchDetailAsync(int batchId, string question, string generatedSql, bool success, string errorMessage = null)
        {
            try
            {
                string query = @"
                    INSERT INTO BatchProcessingDetails (BatchId, Question, GeneratedSql, Success, ErrorMessage)
                    VALUES (@BatchId, @Question, @GeneratedSql, @Success, @ErrorMessage);
                    SELECT SCOPE_IDENTITY();";

                var parameters = new
                {
                    BatchId = batchId,
                    Question = question,
                    GeneratedSql = generatedSql,
                    Success = success,
                    ErrorMessage = errorMessage
                };

                return await _appDbConnection.ExecuteScalarSafeAsync<int>(query, parameters);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Gets recent batch processing jobs.
        /// </summary>
        /// <param name="userId">The user ID (optional).</param>
        /// <param name="limit">Maximum number of jobs to return.</param>
        /// <returns>A list of recent batch processing jobs.</returns>
        public async Task<IEnumerable<BatchProcessingLog>> GetRecentBatchJobsAsync(int? userId = null, int limit = 10)
        {
            try
            {
                string query = userId.HasValue
                    ? @"SELECT TOP (@Limit) * FROM BatchProcessingLogs WHERE UserId = @UserId ORDER BY ProcessedAt DESC"
                    : @"SELECT TOP (@Limit) * FROM BatchProcessingLogs ORDER BY ProcessedAt DESC";

                var parameters = new
                {
                    UserId = userId,
                    Limit = limit
                };

                return await _appDbConnection.QuerySafeAsync<BatchProcessingLog>(query, parameters);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}