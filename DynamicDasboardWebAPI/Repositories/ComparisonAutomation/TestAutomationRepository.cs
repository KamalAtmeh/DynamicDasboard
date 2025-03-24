using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Utilities;
using DynamicDashboardCommon.Models;
using MySqlX.XDevAPI.Relational;
using Mysqlx.Crud;
using System.Collections;

namespace DynamicDasboardWebAPI.Repositories.TestAutomation
{
    /// <summary>
    /// Repository for managing test automation data in the database.
    /// </summary>
    public class TestAutomationRepository : BaseRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestAutomationRepository"/> class.
        /// </summary>
        /// <param name="appDbConnection">Default database connection instance.</param>
        /// <param name="connectionFactory">Factory for dynamic connections.</param>
        public TestAutomationRepository(
            IDbConnection appDbConnection,
            DbConnectionFactory connectionFactory)
            : base(appDbConnection, connectionFactory)
        {
        }

        #region Job Operations

        /// <summary>
        /// Logs a test automation job.
        /// </summary>
        public async Task<int> LogTestJobAsync(string fileName, int databaseId, int totalQuestions,
            int successCount, string llmUsed, decimal avgQueryScore, decimal avgExplanationScore,
            decimal avgDataScore, int? userId = null)
        {
            try
            {
                // Fix the SQL to correctly return the identity value
                const string query = @"
            INSERT INTO TestAutomationJobs (
                FileName, 
                DatabaseSchemaID, 
                TotalQuestions, 
                SuccessCount, 
                AverageQueryMatchScore, 
                AverageExplanationMatchScore, 
                AverageDataMatchScore,
                LLMUsed, 
                ExecutedBy, 
                ExecutedAt
            )
            VALUES (
                @FileName, 
                @DatabaseSchemaID, 
                @TotalQuestions, 
                @SuccessCount, 
                @AverageQueryMatchScore, 
                @AverageExplanationMatchScore, 
                @AverageDataMatchScore,
                @LLMUsed, 
                @ExecutedBy, 
                GETDATE()
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

                var parameters = new
                {
                    FileName = fileName,
                    DatabaseSchemaID = databaseId,
                    TotalQuestions = totalQuestions,
                    SuccessCount = successCount,
                    AverageQueryMatchScore = avgQueryScore,
                    AverageExplanationMatchScore = avgExplanationScore,
                    AverageDataMatchScore = avgDataScore,
                    LLMUsed = llmUsed,
                    ExecutedBy = userId
                };

                var jobID = await WithConnectionAsync(async conn =>
                {
                    return await conn.ExecuteScalarSafeAsync<int>(query, parameters);
                });

                // Log the created job ID for diagnostics
                Console.WriteLine($"Created test job with ID: {jobID}");

                return jobID;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging test job: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Updates an existing test automation job with new details.
        /// </summary>
        /// <param name="jobId">The ID of the job to update.</param>
        /// <param name="totalQuestions">The total number of questions processed.</param>
        /// <param name="successCount">The number of successfully processed questions.</param>
        /// <param name="avgQueryScore">Average SQL query match score.</param>
        /// <param name="avgExplanationScore">Average explanation match score.</param>
        /// <param name="avgDataScore">Average dataset match score.</param>
        /// <returns>The number of affected rows (should be 1 if successful).</returns>
        public async Task<int> UpdateTestJobAsync(
            int jobId,
            int totalQuestions,
            int successCount,
            decimal avgQueryScore,
            decimal avgExplanationScore,
            decimal avgDataScore)
        {
            try
            {
                const string query = @"
            UPDATE TestAutomationJobs 
            SET TotalQuestions = @TotalQuestions, 
                SuccessCount = @SuccessCount, 
                AverageQueryMatchScore = @AverageQueryMatchScore, 
                AverageExplanationMatchScore = @AverageExplanationMatchScore, 
                AverageDataMatchScore = @AverageDataMatchScore
            WHERE JobID = @JobID";

                var parameters = new
                {
                    JobID = jobId,
                    TotalQuestions = totalQuestions,
                    SuccessCount = successCount,
                    AverageQueryMatchScore = avgQueryScore,
                    AverageExplanationMatchScore = avgExplanationScore,
                    AverageDataMatchScore = avgDataScore
                };

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.ExecuteSafeAsync(query, parameters);
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating test job: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves recent test automation jobs.
        /// </summary>
        /// <param name="userId">Filter by user ID (optional).</param>
        /// <param name="limit">Maximum number of jobs to retrieve.</param>
        /// <returns>A collection of test automation jobs.</returns>
        public async Task<IEnumerable<TestAutomationJob>> GetRecentTestJobsAsync(int? userId = null, int limit = 10)
        {
            try
            {
                string query;
                object parameters;

                if (userId.HasValue)
                {
                    query = @"
                        SELECT TOP (@Limit) * 
                        FROM TestAutomationJobs 
                        WHERE ExecutedBy = @UserId 
                        ORDER BY ExecutedAt DESC";

                    parameters = new { UserId = userId.Value, Limit = limit };
                }
                else
                {
                    query = @"
                        SELECT TOP (@Limit) * 
                        FROM TestAutomationJobs 
                        ORDER BY ExecutedAt DESC";

                    parameters = new { Limit = limit };
                }

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.QuerySafeAsync<TestAutomationJob>(query, parameters);
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion

        #region Detail Operations

        /// <summary>
        /// Logs a test detail record.
        /// </summary>
        public async Task<int> LogTestDetailAsync(int jobId, string question, string expectedSql,
            string generatedSql, decimal? sqlMatchScore, string expectedExplanation,
            string generatedExplanation, decimal? explanationMatchScore, int? expectedRowCount,
            int? actualRowCount, decimal? dataMatchScore, string resultMatchStatus,
            string complexityLevel, string queryCategory, int? executionTimeMs,
            bool success, string errorMessage = null)
        {
            try
            {
                // Important validation to prevent mismatched job IDs
                if (jobId <= 0)
                    throw new ArgumentException("Invalid job ID", nameof(jobId));

                // Fix the SQL to correctly return the identity value
                const string query = @"
            INSERT INTO TestAutomationDetails (
                JobID,
                Question,
                ExpectedSQL,
                GeneratedSQL,
                SQLMatchScore,
                ExpectedExplanation,
                GeneratedExplanation,
                ExplanationMatchScore,
                ExpectedRowCount,
                ActualRowCount,
                DataMatchScore,
                ResultMatchStatus,
                ComplexityLevel,
                QueryCategory,
                ExecutionTimeMs,
                Success,
                ErrorMessage
            )
            VALUES (
                @JobID,
                @Question,
                @ExpectedSQL,
                @GeneratedSQL,
                @SQLMatchScore,
                @ExpectedExplanation,
                @GeneratedExplanation,
                @ExplanationMatchScore,
                @ExpectedRowCount,
                @ActualRowCount,
                @DataMatchScore,
                @ResultMatchStatus,
                @ComplexityLevel,
                @QueryCategory,
                @ExecutionTimeMs,
                @Success,
                @ErrorMessage
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

                var parameters = new
                {
                    JobID = jobId,
                    Question = question,
                    ExpectedSQL = expectedSql,
                    GeneratedSQL = generatedSql,
                    SQLMatchScore = sqlMatchScore,
                    ExpectedExplanation = expectedExplanation,
                    GeneratedExplanation = generatedExplanation,
                    ExplanationMatchScore = explanationMatchScore,
                    ExpectedRowCount = expectedRowCount,
                    ActualRowCount = actualRowCount,
                    DataMatchScore = dataMatchScore,
                    ResultMatchStatus = resultMatchStatus,
                    ComplexityLevel = complexityLevel,
                    QueryCategory = queryCategory,
                    ExecutionTimeMs = executionTimeMs,
                    Success = success,
                    ErrorMessage = errorMessage
                };

                var detailId = await WithConnectionAsync(async conn =>
                {
                    return await conn.ExecuteScalarSafeAsync<int>(query, parameters);
                });

                // Log the created detail ID for diagnostics
                Console.WriteLine($"Created test detail with ID: {detailId} for job ID: {jobId}");

                return detailId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging test detail: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves test details for a specific job.
        /// </summary>
        /// <param name="jobId">The ID of the job.</param>
        /// <returns>A collection of test detail records.</returns>
        public async Task<IEnumerable<TestAutomationDetail>> GetTestDetailsForJobAsync(int jobId)
        {
            try
            {
                const string query = @"
                    SELECT * 
                    FROM TestAutomationDetails 
                    WHERE JobID = @JobID 
                    ORDER BY DetailID";

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.QuerySafeAsync<TestAutomationDetail>(query, new { JobID = jobId });
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion

        #region Dataset Operations


        public async Task<int> SaveDatasetAsync(int detailId, bool isExpected, string datasetJson,
            int rowCount, int columnCount, string columnNames, string dataHash)
        {
            try
            {
                // Important validation
                if (detailId <= 0)
                    throw new ArgumentException("Invalid detail ID", nameof(detailId));

                // Check if a dataset already exists for this detail and expected flag
                const string checkQuery = @"
            SELECT COUNT(*) 
            FROM TestAutomationDatasets 
            WHERE DetailID = @DetailID AND IsExpected = @IsExpected";

                var existingCount = await WithConnectionAsync(async conn =>
                {
                    return await conn.ExecuteScalarSafeAsync<int>(checkQuery, new
                    {
                        DetailID = detailId,
                        IsExpected = isExpected
                    });
                });

                if (existingCount > 0)
                {
                    // Update existing dataset
                    const string updateQuery = @"
                UPDATE TestAutomationDatasets 
                SET DatasetJSON = @DatasetJSON,
                    RowCount = @RowCount,
                    ColumnCount = @ColumnCount,
                    ColumnNames = @ColumnNames,
                    DataHash = @DataHash
                WHERE DetailID = @DetailID AND IsExpected = @IsExpected;
                
                SELECT DatasetID FROM TestAutomationDatasets 
                WHERE DetailID = @DetailID AND IsExpected = @IsExpected;";

                    return await WithConnectionAsync(async conn =>
                    {
                        return await conn.ExecuteScalarSafeAsync<int>(updateQuery, new
                        {
                            DetailID = detailId,
                            IsExpected = isExpected,
                            DatasetJSON = datasetJson,
                            RowCount = rowCount,
                            ColumnCount = columnCount,
                            ColumnNames = columnNames,
                            DataHash = dataHash
                        });
                    });
                }
                else
                {
                    // Create new dataset
                    const string insertQuery = @"
                INSERT INTO TestAutomationDatasets (
                    DetailID,
                    IsExpected,
                    DatasetJSON,
                    RowCount,
                    ColumnCount,
                    ColumnNames,
                    DataHash
                )
                VALUES (
                    @DetailID,
                    @IsExpected,
                    @DatasetJSON,
                    @RowCount,
                    @ColumnCount,
                    @ColumnNames,
                    @DataHash
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    return await WithConnectionAsync(async conn =>
                    {
                        return await conn.ExecuteScalarSafeAsync<int>(insertQuery, new
                        {
                            DetailID = detailId,
                            IsExpected = isExpected,
                            DatasetJSON = datasetJson,
                            RowCount = rowCount,
                            ColumnCount = columnCount,
                            ColumnNames = columnNames,
                            DataHash = dataHash
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving dataset: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves a dataset for a test detail.
        /// </summary>
        /// <param name="detailId">The ID of the test detail.</param>
        /// <param name="isExpected">Whether to retrieve the expected dataset (true) or actual dataset (false).</param>
        /// <returns>The dataset record.</returns>
        public async Task<TestAutomationDataset> GetDatasetAsync(int detailId, bool isExpected)
        {
            try
            {
                const string query = @"
                    SELECT * 
                    FROM TestAutomationDatasets 
                    WHERE DetailID = @DetailID AND IsExpected = @IsExpected";

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.QuerySingleOrDefaultSafeAsync<TestAutomationDataset>(
                        query,
                        new { DetailID = detailId, IsExpected = isExpected });
                });
            }
            catch (Exception)
            {
                throw;
            }
        }


        /// <summary>
        /// Retrieves test details with pagination and total count.
        /// </summary>
        /// <param name="jobId">The job ID.</param>
        /// <param name="pageNumber">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns>Tuple containing paginated test details and total count.</returns>
        public async Task<(IEnumerable<TestAutomationDetail> Data, int TotalCount)> GetTestDetailsPaginatedAsync(
            int jobId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                const string queryData = @"
            SELECT *
            FROM TestAutomationDetails 
            WHERE JobID = @JobID 
            ORDER BY DetailID
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY";

                const string queryCount = @"
            SELECT COUNT(*)
            FROM TestAutomationDetails 
            WHERE JobID = @JobID";

                return await WithConnectionAsync(async conn =>
                {
                    var data = await conn.QuerySafeAsync<TestAutomationDetail>(queryData, new
                    {
                        JobID = jobId,
                        Offset = (pageNumber - 1) * pageSize,
                        PageSize = pageSize
                    });

                    var totalCount = await conn.ExecuteScalarSafeAsync<int>(queryCount, new { JobID = jobId });

                    return (data, totalCount);
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion
    }
}