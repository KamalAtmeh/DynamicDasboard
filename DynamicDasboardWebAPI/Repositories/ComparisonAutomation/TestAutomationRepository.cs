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
        /// <param name="fileName">The name of the test file.</param>
        /// <param name="databaseId">The ID of the database schema used for testing.</param>
        /// <param name="totalQuestions">The total number of questions in the test.</param>
        /// <param name="successCount">The number of successfully processed questions.</param>
        /// <param name="llmUsed">The LLM provider used for the test.</param>
        /// <param name="avgQueryScore">Average SQL query match score.</param>
        /// <param name="avgExplanationScore">Average explanation match score.</param>
        /// <param name="avgDataScore">Average dataset match score.</param>
        /// <param name="userId">The user who executed the test (optional).</param>
        /// <returns>The ID of the newly created job.</returns>
        public async Task<int> LogTestJobAsync(
            string fileName,
            int databaseId,
            int totalQuestions,
            int successCount,
            string llmUsed,
            decimal avgQueryScore,
            decimal avgExplanationScore,
            decimal avgDataScore,
            int? userId = null)
        {
            try
            {
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
                    SELECT CAST(SCOPE_IDENTITY() AS INT) + 1;";

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

                return jobID;
            }
            catch (Exception)
            {
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
        /// <param name="jobId">The ID of the parent job.</param>
        /// <param name="question">The natural language question.</param>
        /// <param name="expectedSql">The expected SQL query.</param>
        /// <param name="generatedSql">The generated SQL query.</param>
        /// <param name="sqlMatchScore">The SQL match score.</param>
        /// <param name="expectedExplanation">The expected explanation.</param>
        /// <param name="generatedExplanation">The generated explanation.</param>
        /// <param name="explanationMatchScore">The explanation match score.</param>
        /// <param name="expectedRowCount">The expected row count.</param>
        /// <param name="actualRowCount">The actual row count.</param>
        /// <param name="dataMatchScore">The dataset match score.</param>
        /// <param name="resultMatchStatus">The result match status.</param>
        /// <param name="complexityLevel">The question complexity level.</param>
        /// <param name="queryCategory">The query category.</param>
        /// <param name="executionTimeMs">The execution time in milliseconds.</param>
        /// <param name="success">Whether the test was successful.</param>
        /// <param name="errorMessage">Any error message.</param>
        /// <returns>The ID of the newly created detail record.</returns>
        public async Task<int> LogTestDetailAsync(
            int jobId,
            string question,
            string expectedSql,
            string generatedSql,
            decimal? sqlMatchScore,
            string expectedExplanation,
            string generatedExplanation,
            decimal? explanationMatchScore,
            int? expectedRowCount,
            int? actualRowCount,
            decimal? dataMatchScore,
            string resultMatchStatus,
            string complexityLevel,
            string queryCategory,
            int? executionTimeMs,
            bool success,
            string errorMessage = null)
        {
            try
            {
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
                    SELECT CAST(SCOPE_IDENTITY() AS INT) + 1;";

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

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.ExecuteScalarSafeAsync<int>(query, parameters);
                });
            }
            catch (Exception)
            {
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

        /// <summary>
        /// Saves a dataset for a test detail.
        /// </summary>
        /// <param name="detailId">The ID of the test detail.</param>
        /// <param name="isExpected">Whether this is the expected dataset.</param>
        /// <param name="datasetJson">JSON representation of the dataset.</param>
        /// <param name="rowCount">Number of rows in the dataset.</param>
        /// <param name="columnCount">Number of columns in the dataset.</param>
        /// <param name="columnNames">JSON array of column names.</param>
        /// <param name="dataHash">Hash of the dataset for comparison.</param>
        /// <returns>The ID of the newly created dataset record.</returns>
        public async Task<int> SaveDatasetAsync(
            int detailId,
            bool isExpected,
            string datasetJson,
            int rowCount,
            int columnCount,
            string columnNames,
            string dataHash)
        {
            try
            {
                const string query = @"
                    INSERT INTO TestAutomationDatasets (
                        DetailID,
                        IsExpected,
                        DatasetJSON,
                        [RowCount],
                        [ColumnCount],
                        ColumnNames,
                        [DataHash]
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
                    SELECT CAST(SCOPE_IDENTITY() AS INT) + 1;";

                var parameters = new
                {
                    DetailID = detailId,
                    IsExpected = isExpected,
                    DatasetJSON = datasetJson,
                    RowCount = rowCount,
                    ColumnCount = columnCount,
                    ColumnNames = columnNames,
                    DataHash = dataHash
                };

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.ExecuteScalarSafeAsync<int>(query, parameters);
                });
            }
            catch (Exception)
            {
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


        // Add to DynamicDasboardWebAPI/Repositories/TestAutomation/TestAutomationRepository.cs

        /// <summary>
        /// Retrieves test details by job ID and row number (for pagination).
        /// </summary>
        /// <param name="jobId">The ID of the job.</param>
        /// <param name="pageNumber">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns>A collection of test detail records with pagination.</returns>
        public async Task<IEnumerable<TestAutomationDetail>> GetTestDetailsPaginatedAsync(
            int jobId, int pageNumber = 1, int pageSize = 20)
        {
            try
            {


                const string query = @"
            SELECT *
            FROM TestAutomationDetails 
            WHERE JobID = @JobID 
            ORDER BY DetailID
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY"
                ;

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.QuerySafeAsync<TestAutomationDetail>(query, new
                    {
                        JobID = jobId,
                        Offset = (pageNumber - 1) * pageSize,
                        PageSize = pageSize
                    });
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