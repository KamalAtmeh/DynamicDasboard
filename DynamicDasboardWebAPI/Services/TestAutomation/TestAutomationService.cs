using DynamicDasboardWebAPI.Repositories;
using DynamicDasboardWebAPI.Repositories.TestAutomation;
using DynamicDasboardWebAPI.Services.LLM;
using DynamicDasboardWebAPI.Utilities;
using DynamicDashboardCommon.Models;
using DynamicDashboardCommon.Models.TestAutomation;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Services.TestAutomation
{
    /// <summary>
    /// Service for managing test automation operations.
    /// Provides functionality for running test cases, comparing results, and generating reports.
    /// </summary>
    public class TestAutomationService
    {
        private readonly TestAutomationRepository objTestAutomationRepostiroy;
        private readonly QueryRepository _queryRepository;
        private readonly LLMServiceFactory _llmServiceFactory;
        private readonly DatabaseService _databaseService;
        private readonly DatabaseSchemaService _databaseSchemaService;
        private readonly DatasetComparisonService _datasetComparisonService;
        private readonly IConfiguration _configuration;

        // Configurable settings from appsettings.json
        private readonly int _maxRecordsToCompare;
        private readonly int _requestTimeoutSeconds;
        private readonly int _pageSizeDefault;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestAutomationService"/> class.
        /// </summary>
        /// <param name="testRepository">Repository for test automation data.</param>
        /// <param name="queryRepository">Repository for executing queries.</param>
        /// <param name="llmServiceFactory">Factory for creating LLM service instances.</param>
        /// <param name="databaseService">Service for database operations.</param>
        /// <param name="databaseSchemaService">Service for database schema operations.</param>
        /// <param name="datasetComparisonService">Service for dataset comparison.</param>
        /// <param name="configuration">Application configuration.</param>
        public TestAutomationService(
            TestAutomationRepository testRepository,
            QueryRepository queryRepository,
            LLMServiceFactory llmServiceFactory,
            DatabaseService databaseService,
            DatabaseSchemaService databaseSchemaService,
            DatasetComparisonService datasetComparisonService,
            IConfiguration configuration)
        {
            objTestAutomationRepostiroy = testRepository ?? throw new ArgumentNullException(nameof(testRepository));
            _queryRepository = queryRepository ?? throw new ArgumentNullException(nameof(queryRepository));
            _llmServiceFactory = llmServiceFactory ?? throw new ArgumentNullException(nameof(llmServiceFactory));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _databaseSchemaService = databaseSchemaService ?? throw new ArgumentNullException(nameof(databaseSchemaService));
            _datasetComparisonService = datasetComparisonService ?? throw new ArgumentNullException(nameof(datasetComparisonService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            // Get configurable comparison limit from settings (default values if not specified)
            _maxRecordsToCompare = _configuration.GetValue<int>("TestAutomation:MaxRecordsToCompare", 100);
            _requestTimeoutSeconds = _configuration.GetValue<int>("TestAutomation:RequestTimeoutSeconds", 300);
            _pageSizeDefault = _configuration.GetValue<int>("TestAutomation:DefaultPageSize", 10);

            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        /// <summary>
        /// Processes a test file and runs comparison on datasets only.
        /// </summary>
        /// <param name="fileStream">The file stream containing test cases.</param>
        /// <param name="databaseId">The database ID to test against.</param>
        /// <param name="llmProvider">The LLM provider to use.</param>
        /// <param name="userId">Optional user ID.</param>
        /// <returns>The processed file as a byte array.</returns>
        public async Task<byte[]> ProcessTestCasesFileAsync(
            Stream fileStream, int databaseId, string llmProvider, int? userId = null)
        {
            try
            {
                // Initialize Excel package
                using var package = new ExcelPackage(fileStream);
                var worksheet = package.Workbook.Worksheets[0];

                // Check if the worksheet has the expected structure
                ValidateWorksheetStructure(worksheet);

                // Define column indices based on updated structure (removed SQL and Explanation match scores)
                const int questionCol = 1;          // Column A
                const int expectedSqlCol = 2;       // Column B
                const int expectedExplanationCol = 3; // Column C
                const int complexityLevelCol = 4;   // Column D
                const int queryCategoryCol = 5;     // Column E
                const int expectedRowCountCol = 6;  // Column F

                // Output columns - Updated to remove SQL and Explanation match scores
                const int generatedSqlCol = 7;      // Column G
                const int actualSqlCol = 8;         // Column H - NEW COLUMN
                const int generatedExplanationCol = 9; // Column I
                const int actualRowCountCol = 10;   // Column J
                const int dataMatchScoreCol = 11;   // Column K
                const int resultMatchStatusCol = 12; // Column L
                const int executionTimeCol = 13;    // Column M
                const int llmUsedCol = 14;          // Column N
                const int statusCol = 15;           // Column O
                const int errorCol = 16;            // Column P

                // Find the last row with data
                int rowCount = FindLastDataRow(worksheet);

                // Initialize tracking variables
                int totalQuestions = 0;
                int successCount = 0;
                decimal totalDataMatchScore = 0;

                string fileName = "TestAutomation_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx";

                // Create job record with initial values - we'll update these later
                int jobId = await objTestAutomationRepostiroy.LogTestJobAsync(
                    fileName,
                    databaseId,
                    0, // Initialize with 0, will update later
                    0, // Initialize with 0, will update later
                    llmProvider,
                    0, // SQL match score - removed, kept for DB compatibility
                    0, // Explanation match score - removed, kept for DB compatibility
                    0, // Data match score - will update later
                    userId
                );

                // Get LLM service instance
                var llmService = _llmServiceFactory.CreateLlmService();

                // Get database info for schema
                var database = await _databaseService.GetDatabaseByIdAsync(databaseId);
                if (database == null)
                {
                    throw new ArgumentException($"Database with ID {databaseId} not found");
                }

                // Get database schema for LLM context
                var dbSchema = await _databaseSchemaService.GetSchemaObject(database.DatabaseID);
                if (dbSchema == null)
                {
                    throw new ArgumentException("Could not retrieve database schema");
                }

                var schemaStr = _databaseSchemaService.BuildOptimizedSchemaString(dbSchema);

                // Process each row
                for (int row = 2; row <= rowCount; row++) // Assuming row 1 is header
                {
                    var question = worksheet.Cells[row, questionCol].Text;
                    if (string.IsNullOrWhiteSpace(question))
                        continue;

                    totalQuestions++;
                    var startTime = DateTime.Now;
                    int detailId = 0;

                    try
                    {
                        // Get expected values from worksheet
                        var expectedSql = worksheet.Cells[row, expectedSqlCol].Text;
                        var expectedExplanation = worksheet.Cells[row, expectedExplanationCol].Text;
                        var complexityLevel = worksheet.Cells[row, complexityLevelCol].Text;
                        var queryCategory = worksheet.Cells[row, queryCategoryCol].Text;

                        // Step 1: Process the natural language query using the specified LLM
                        var request = new NlQueryRequest
                        {
                            Question = question,
                            DatabaseId = databaseId
                        };

                        // Generate SQL with explanation
                        var sqlExplanationResponse = await llmService.GenerateSqlWithExplanationAsync(
                            request.Question,
                            schemaStr,
                            null);

                        var generatedSql = sqlExplanationResponse.SqlQuery;
                        var generatedExplanation = sqlExplanationResponse.BusinessExplanation;
                        string actualSql = string.Empty; // Will store the actual executed SQL

                        // Execute the expected SQL query if provided
                        List<Dictionary<string, object>> expectedDataset = null;
                        int? expectedRowCount = null;
                        if (!string.IsNullOrWhiteSpace(expectedSql))
                        {
                            try
                            {
                                expectedDataset = await _queryRepository.ExecuteQueryOnDatabaseAsync(expectedSql, databaseId);
                                expectedRowCount = expectedDataset?.Count;
                                worksheet.Cells[row, expectedRowCountCol].Value = expectedRowCount;
                            }
                            catch (Exception ex)
                            {
                                // Add source file, method name, and line number to error
                                string errorDetails = GetDetailedExceptionInfo(ex);
                                worksheet.Cells[row, errorCol].Value = $"Expected SQL execution failed: {ex.Message} | {errorDetails}";
                            }
                        }

                        // Execute the generated SQL query
                        List<Dictionary<string, object>> actualDataset = null;
                        int? actualRowCount = null;
                        if (!string.IsNullOrWhiteSpace(generatedSql))
                        {
                            try
                            {
                                // Store the actual SQL that will be executed
                                actualSql = generatedSql;

                                // Execute the SQL
                                actualDataset = await _queryRepository.ExecuteQueryOnDatabaseAsync(generatedSql, databaseId);
                                actualRowCount = actualDataset?.Count;
                            }
                            catch (Exception ex)
                            {
                                // Add source file, method name, and line number to error
                                string errorDetails = GetDetailedExceptionInfo(ex);
                                worksheet.Cells[row, errorCol].Value = $"Generated SQL execution failed: {ex.Message} | {errorDetails}";
                            }
                        }

                        // Compare datasets using DatasetComparisonService
                        decimal dataMatchScore = 0;
                        string resultMatchStatus = "Not Compared";

                        if (expectedDataset != null && actualDataset != null)
                        {
                            var comparisonResult = _datasetComparisonService.CompareDatasets(expectedDataset, actualDataset);
                            dataMatchScore = comparisonResult.IsEquivalent ? 1.0m : 0.0m;
                            resultMatchStatus = comparisonResult.ComparisonSummary;

                            // Add dataset match score to total for averaging
                            totalDataMatchScore += dataMatchScore;
                        }
                        else
                        {
                            // If one or both datasets are empty, provide clear error message
                            if (expectedDataset == null && actualDataset == null)
                            {
                                resultMatchStatus = "Both datasets are empty";
                                worksheet.Cells[row, errorCol].Value = "Both expected and actual datasets are empty. Check SQL execution.";
                            }
                            else if (expectedDataset == null)
                            {
                                resultMatchStatus = "Expected dataset is empty";
                                if (string.IsNullOrEmpty(worksheet.Cells[row, errorCol].Text))
                                {
                                    worksheet.Cells[row, errorCol].Value = "Expected dataset is empty. Check expected SQL.";
                                }
                            }
                            else // actualDataset is null
                            {
                                resultMatchStatus = "Actual dataset is empty";
                                if (string.IsNullOrEmpty(worksheet.Cells[row, errorCol].Text))
                                {
                                    worksheet.Cells[row, errorCol].Value = "Actual dataset is empty. Check generated SQL.";
                                }
                            }
                        }

                        // Calculate execution time
                        var endTime = DateTime.Now;
                        var executionTimeMs = (int)(endTime - startTime).TotalMilliseconds;

                        // Determine success based on dataset comparison only
                        bool success = dataMatchScore > 0.9m;

                        // Create test detail record
                        detailId = await objTestAutomationRepostiroy.LogTestDetailAsync(
                            jobId,
                            question,
                            expectedSql,
                            generatedSql,
                            null, // Placeholder value - SQL match removed
                            expectedExplanation,
                            generatedExplanation,
                            null, // Placeholder value - Explanation match removed
                            expectedRowCount,
                            actualRowCount,
                            dataMatchScore,
                            resultMatchStatus,
                            complexityLevel,
                            queryCategory,
                            executionTimeMs,
                            success,
                            worksheet.Cells[row, errorCol].Text
                        );

                        // Store datasets for comparison
                        if (expectedDataset != null)
                        {
                            await StoreDatasetAsync(detailId, true, expectedDataset);
                        }
                        else
                        {
                            await StoreDatasetAsync(detailId, true, new List<Dictionary<string, object>>());
                        }

                        if (actualDataset != null)
                        {
                            await StoreDatasetAsync(detailId, false, actualDataset);
                        }
                        else
                        {
                            await StoreDatasetAsync(detailId, false, new List<Dictionary<string, object>>());
                        }

                        // Update Excel with results - Updated column mapping
                        worksheet.Cells[row, generatedSqlCol].Value = generatedSql;
                        worksheet.Cells[row, actualSqlCol].Value = actualSql; // New column for actual executed SQL
                        worksheet.Cells[row, generatedExplanationCol].Value = generatedExplanation;
                        worksheet.Cells[row, actualRowCountCol].Value = actualRowCount;
                        worksheet.Cells[row, dataMatchScoreCol].Value = dataMatchScore;
                        worksheet.Cells[row, resultMatchStatusCol].Value = resultMatchStatus;
                        worksheet.Cells[row, executionTimeCol].Value = executionTimeMs;
                        worksheet.Cells[row, llmUsedCol].Value = llmProvider;
                        worksheet.Cells[row, statusCol].Value = success ? "Success" : "Failed";

                        // Update running totals
                        if (success)
                        {
                            successCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Handle errors and log them with detailed info
                        string errorDetails = GetDetailedExceptionInfo(ex);

                        worksheet.Cells[row, statusCol].Value = "Error";
                        worksheet.Cells[row, errorCol].Value = $"{ex.Message} | {errorDetails}";

                        // Log error to database if we haven't created a detail record yet
                        if (detailId == 0)
                        {
                            try
                            {
                                detailId = await objTestAutomationRepostiroy.LogTestDetailAsync(
                                    jobId,
                                    question,
                                    worksheet.Cells[row, expectedSqlCol].Text,
                                    null, // Generated SQL
                                    null, // SQL match score - removed
                                    worksheet.Cells[row, expectedExplanationCol].Text,
                                    null, // Generated explanation
                                    null, // Explanation match score - removed
                                    null, // Expected row count
                                    null, // Actual row count
                                    null, // Data match score
                                    "Error", // Result match status
                                    worksheet.Cells[row, complexityLevelCol].Text,
                                    worksheet.Cells[row, queryCategoryCol].Text,
                                    null, // Execution time
                                    false, // Success flag
                                    $"{ex.Message} | {errorDetails}" // Error message with details
                                );

                                // Store empty datasets to ensure records exist
                                await StoreDatasetAsync(detailId, true, new List<Dictionary<string, object>>());
                                await StoreDatasetAsync(detailId, false, new List<Dictionary<string, object>>());
                            }
                            catch (Exception innerEx)
                            {
                                string innerErrorDetails = GetDetailedExceptionInfo(innerEx);
                                worksheet.Cells[row, errorCol].Value += $" | Error creating test detail record: {innerEx.Message} | {innerErrorDetails}";
                            }
                        }
                    }
                }

                // Update the job with final counts and averages - keep SQL/Explanation scores as 0 for DB compatibility
                decimal avgDataScore = successCount > 0 ? totalDataMatchScore / successCount : 0;

                await objTestAutomationRepostiroy.UpdateTestJobAsync(
                    jobId,
                    totalQuestions,
                    successCount,
                    0, // SQL match score - removed but kept for DB
                    0, // Explanation match score - removed but kept for DB
                    avgDataScore
                );

                // Format the Excel file
                FormatExcelOutput(worksheet, rowCount);

                // Return the modified Excel file
                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                string errorDetails = GetDetailedExceptionInfo(ex);
                throw new Exception($"Error processing test cases: {ex.Message} | {errorDetails}", ex);
            }
        }

        /// <summary>
        /// Gets detailed exception information including file, method, and line number.
        /// </summary>
        /// <param name="ex">The exception to get details for.</param>
        /// <returns>A string with detailed exception information.</returns>
        private string GetDetailedExceptionInfo(Exception ex)
        {
            try
            {
                var stackTrace = new StackTrace(ex, true);
                var frame = stackTrace.GetFrame(0);

                if (frame != null)
                {
                    string fileName = Path.GetFileName(frame.GetFileName() ?? "Unknown");
                    string methodName = frame.GetMethod()?.Name ?? "Unknown";
                    int lineNumber = frame.GetFileLineNumber();

                    return $"File: {fileName}, Method: {methodName}, Line: {lineNumber}";
                }

                return "Details unavailable";
            }
            catch
            {
                return "Error extracting exception details";
            }
        }

        /// <summary>
        /// Stores a dataset for comparison in the database with enhanced error handling.
        /// </summary>
        private async Task<int> StoreDatasetAsync(int detailId, bool isExpected, List<Dictionary<string, object>> dataset)
        {
            if (detailId <= 0)
            {
                throw new ArgumentException("Invalid detail ID");
            }

            try
            {
                // Ensure dataset is not null
                dataset = dataset ?? new List<Dictionary<string, object>>();

                // Always normalize the dataset, even if empty
                var normalizedDataset = NormalizeDatasetForStorage(dataset);

                // Serialize with consistent settings
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                // Create an empty JSON array if dataset is empty
                string datasetJson = normalizedDataset.Any()
                    ? JsonSerializer.Serialize(normalizedDataset, options)
                    : "[]";

                // Get column names or empty array
                List<string> columnNames = normalizedDataset.Count > 0
                    ? normalizedDataset[0].Keys.ToList()
                    : new List<string>();

                string columnNamesJson = JsonSerializer.Serialize(columnNames, options);

                // Compute hash for quick comparison (or use empty string for empty dataset)
                string dataHash = normalizedDataset.Any()
                    ? ComparisonUtilities.ComputeDatasetHash(normalizedDataset)
                    : string.Empty;

                // Store in database with retry logic
                int retryCount = 3;
                int datasetId = -1;

                for (int attempt = 1; attempt <= retryCount; attempt++)
                {
                    try
                    {
                        datasetId = await objTestAutomationRepostiroy.SaveDatasetAsync(
                            detailId,
                            isExpected,
                            datasetJson,
                            normalizedDataset.Count,
                            columnNames.Count,
                            columnNamesJson,
                            dataHash
                        );

                        // If successful, break the retry loop
                        if (datasetId > 0)
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (attempt < retryCount)
                        {
                            // Log retry attempt
                            Console.WriteLine($"Attempt {attempt} failed to store dataset. Retrying. Error: {ex.Message}");
                            await Task.Delay(500 * attempt); // Exponential backoff
                        }
                        else
                        {
                            // Log final failure
                            string errorDetails = GetDetailedExceptionInfo(ex);
                            Console.WriteLine($"All attempts to store dataset failed. Error: {ex.Message} | {errorDetails}");
                            throw; // Rethrow on final attempt
                        }
                    }
                }

                return datasetId;
            }
            catch (Exception ex)
            {
                string errorDetails = GetDetailedExceptionInfo(ex);
                Console.WriteLine($"Error storing dataset: {ex.Message} | {errorDetails}");
                throw; // Rethrow to ensure caller knows about failure
            }
        }

        /// <summary>
        /// Generates a test template Excel file with updated column structure.
        /// </summary>
        /// <returns>Excel file as byte array.</returns>
        public byte[] GenerateTestTemplate()
        {
            try
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Test Cases");

                // Define headers according to updated structure (removed SQL/Explanation match scores)
                worksheet.Cells[1, 1].Value = "Question";
                worksheet.Cells[1, 2].Value = "ExpectedSQL";
                worksheet.Cells[1, 3].Value = "ExpectedExplanation";
                worksheet.Cells[1, 4].Value = "ComplexityLevel";
                worksheet.Cells[1, 5].Value = "QueryCategory";
                worksheet.Cells[1, 6].Value = "ExpectedRowCount";
                worksheet.Cells[1, 7].Value = "GeneratedSQL";
                worksheet.Cells[1, 8].Value = "ActualSQL"; // New column
                worksheet.Cells[1, 9].Value = "GeneratedExplanation";
                worksheet.Cells[1, 10].Value = "ActualRowCount";
                worksheet.Cells[1, 11].Value = "DataMatchScore";
                worksheet.Cells[1, 12].Value = "ResultMatchStatus";
                worksheet.Cells[1, 13].Value = "ExecutionTimeMs";
                worksheet.Cells[1, 14].Value = "LLMUsed";
                worksheet.Cells[1, 15].Value = "Status";
                worksheet.Cells[1, 16].Value = "ErrorMessage";

                // Format headers
                using (var range = worksheet.Cells[1, 1, 1, 16])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // Add dropdown lists for complexity level and query category
                var complexityValidation = worksheet.DataValidations.AddListValidation("D2:D1000");
                complexityValidation.Formula.Values.Add("Simple");
                complexityValidation.Formula.Values.Add("Medium");
                complexityValidation.Formula.Values.Add("Complex");
                complexityValidation.Formula.Values.Add("Very Complex");

                var categoryValidation = worksheet.DataValidations.AddListValidation("E2:E1000");
                categoryValidation.Formula.Values.Add("Aggregate");
                categoryValidation.Formula.Values.Add("Filter");
                categoryValidation.Formula.Values.Add("Join");
                categoryValidation.Formula.Values.Add("GroupBy");
                categoryValidation.Formula.Values.Add("OrderBy");
                categoryValidation.Formula.Values.Add("SubQuery");
                categoryValidation.Formula.Values.Add("CTE");
                categoryValidation.Formula.Values.Add("Complex");

                // Add number validation for ExpectedRowCount
                var expectedRowCountValidation = worksheet.DataValidations.AddIntegerValidation("F2:F1000");
                expectedRowCountValidation.Operator = OfficeOpenXml.DataValidation.ExcelDataValidationOperator.greaterThanOrEqual;
                expectedRowCountValidation.Formula.Value = 0;
                expectedRowCountValidation.AllowBlank = true;

                // Set column widths - Updated column layout
                worksheet.Column(1).Width = 40;  // Question
                worksheet.Column(2).Width = 50;  // ExpectedSQL
                worksheet.Column(3).Width = 50;  // ExpectedExplanation
                worksheet.Column(4).Width = 15;  // ComplexityLevel
                worksheet.Column(5).Width = 15;  // QueryCategory
                worksheet.Column(6).Width = 15;  // ExpectedRowCount
                worksheet.Column(7).Width = 50;  // GeneratedSQL
                worksheet.Column(8).Width = 50;  // ActualSQL
                worksheet.Column(9).Width = 50;  // GeneratedExplanation
                worksheet.Column(10).Width = 15; // ActualRowCount
                worksheet.Column(11).Width = 15; // DataMatchScore
                worksheet.Column(12).Width = 20; // ResultMatchStatus
                worksheet.Column(13).Width = 15; // ExecutionTimeMs
                worksheet.Column(14).Width = 15; // LLMUsed
                worksheet.Column(15).Width = 15; // Status
                worksheet.Column(16).Width = 50; // ErrorMessage

                // Add instructions row with comments
                int row = 2;
                worksheet.Cells[row, 1].Value = "Enter your natural language question here";
                worksheet.Cells[row, 2].Value = "Enter the expected SQL query result";
                worksheet.Cells[row, 3].Value = "Enter the expected explanation";
                worksheet.Cells[row, 4].Value = "Select complexity";
                worksheet.Cells[row, 5].Value = "Select category";
                worksheet.Cells[row, 6].Value = "Enter expected row count (optional)";

                // Apply instruction formatting
                using (var range = worksheet.Cells[row, 1, row, 6])
                {
                    range.Style.Font.Italic = true;
                    range.Style.Font.Color.SetColor(System.Drawing.Color.Gray);
                }

                // Protect cells that should not be edited
                worksheet.Cells[$"G3:P1000"].Style.Locked = true;

                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                string errorDetails = GetDetailedExceptionInfo(ex);
                throw new Exception($"Error generating test template: {ex.Message} | {errorDetails}", ex);
            }
        }

        /// <summary>
        /// Generates a sample file with 50 test questions for the specified database.
        /// </summary>
        /// <returns>Excel file with sample test questions as byte array.</returns>
        public byte[] Generate50TestQuestionsFile()
        {
            try
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Test Questions");

                // Add headers - Updated column layout
                worksheet.Cells[1, 1].Value = "Question";
                worksheet.Cells[1, 2].Value = "ExpectedSQL";
                worksheet.Cells[1, 3].Value = "ExpectedExplanation";
                worksheet.Cells[1, 4].Value = "ComplexityLevel";
                worksheet.Cells[1, 5].Value = "QueryCategory";
                worksheet.Cells[1, 6].Value = "ExpectedRowCount";
                worksheet.Cells[1, 7].Value = "GeneratedSQL";
                worksheet.Cells[1, 8].Value = "ActualSQL"; // New column
                worksheet.Cells[1, 9].Value = "GeneratedExplanation";
                worksheet.Cells[1, 10].Value = "ActualRowCount";
                worksheet.Cells[1, 11].Value = "DataMatchScore";
                worksheet.Cells[1, 12].Value = "ResultMatchStatus";
                worksheet.Cells[1, 13].Value = "ExecutionTimeMs";
                worksheet.Cells[1, 14].Value = "LLMUsed";
                worksheet.Cells[1, 15].Value = "Status";
                worksheet.Cells[1, 16].Value = "ErrorMessage";

                // Format headers
                using (var range = worksheet.Cells[1, 1, 1, 16])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // Add the 50 test questions from business document
                string[] questions = Get50TestQuestions();
                for (int i = 0; i < questions.Length; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = questions[i];

                    // Add default values for complexity and category based on question type
                    // This is just for user convenience - they should fill in the expected results
                    if (questions[i].Contains("top") || questions[i].Contains("most") || questions[i].Contains("highest"))
                    {
                        worksheet.Cells[i + 2, 4].Value = "Medium";
                        worksheet.Cells[i + 2, 5].Value = "Aggregate";
                    }
                    else if (questions[i].Contains("join") || questions[i].Contains("both") || questions[i].Contains("related"))
                    {
                        worksheet.Cells[i + 2, 4].Value = "Medium";
                        worksheet.Cells[i + 2, 5].Value = "Join";
                    }
                    else if (questions[i].Contains("group") || questions[i].Contains("average") || questions[i].Contains("sum"))
                    {
                        worksheet.Cells[i + 2, 4].Value = "Medium";
                        worksheet.Cells[i + 2, 5].Value = "GroupBy";
                    }
                    else if (questions[i].Contains("order") || questions[i].Contains("sort"))
                    {
                        worksheet.Cells[i + 2, 4].Value = "Simple";
                        worksheet.Cells[i + 2, 5].Value = "OrderBy";
                    }
                    else
                    {
                        worksheet.Cells[i + 2, 4].Value = "Simple";
                        worksheet.Cells[i + 2, 5].Value = "Filter";
                    }
                }

                // Set column widths - Updated column layout
                worksheet.Column(1).Width = 50;  // Question
                worksheet.Column(2).Width = 70;  // ExpectedSQL
                worksheet.Column(3).Width = 50;  // ExpectedExplanation
                worksheet.Column(4).Width = 15;  // ComplexityLevel
                worksheet.Column(5).Width = 15;  // QueryCategory
                worksheet.Column(6).Width = 15;  // ExpectedRowCount
                worksheet.Column(7).Width = 70;  // GeneratedSQL
                worksheet.Column(8).Width = 70;  // ActualSQL
                worksheet.Column(9).Width = 50;  // GeneratedExplanation
                worksheet.Column(10).Width = 15; // ActualRowCount
                worksheet.Column(11).Width = 15; // DataMatchScore
                worksheet.Column(12).Width = 20; // ResultMatchStatus
                worksheet.Column(13).Width = 15; // ExecutionTimeMs
                worksheet.Column(14).Width = 15; // LLMUsed
                worksheet.Column(15).Width = 15; // Status
                worksheet.Column(16).Width = 50; // ErrorMessage

                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                string errorDetails = GetDetailedExceptionInfo(ex);
                throw new Exception($"Error generating sample test questions: {ex.Message} | {errorDetails}", ex);
            }
        }

        /// <summary>
        /// Retrieves recent test automation jobs.
        /// </summary>
        /// <param name="userId">Optional user ID filter.</param>
        /// <param name="limit">Maximum number of jobs to retrieve.</param>
        /// <returns>Collection of test automation jobs.</returns>
        public async Task<IEnumerable<TestAutomationJob>> GetRecentJobsAsync(int? userId = null, int limit = 10)
        {
            try
            {
                return await objTestAutomationRepostiroy.GetRecentTestJobsAsync(userId, limit);
            }
            catch (Exception ex)
            {
                string errorDetails = GetDetailedExceptionInfo(ex);
                throw new Exception($"Error retrieving recent test jobs: {ex.Message} | {errorDetails}", ex);
            }
        }

        /// <summary>
        /// Retrieves details of a specific test job.
        /// </summary>
        /// <param name="jobId">The ID of the job to retrieve.</param>
        /// <returns>Collection of test details for the specified job.</returns>
        public async Task<IEnumerable<TestAutomationDetail>> GetJobDetailsAsync(int jobId)
        {
            try
            {
                return await objTestAutomationRepostiroy.GetTestDetailsForJobAsync(jobId);
            }
            catch (Exception ex)
            {
                string errorDetails = GetDetailedExceptionInfo(ex);
                throw new Exception($"Error retrieving test job details: {ex.Message} | {errorDetails}", ex);
            }
        }

        /// <summary>
        /// Converts a JSON test cases request to an Excel template file.
        /// </summary>
        /// <param name="request">The JSON request containing test cases.</param>
        /// <returns>The Excel file as a byte array.</returns>
        public byte[] ConvertJsonToExcelTemplate(TestCasesImportRequest request)
        {
            try
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Test Cases");

                // Add headers according to the updated template layout
                worksheet.Cells[1, 1].Value = "Question";
                worksheet.Cells[1, 2].Value = "ExpectedSQL";
                worksheet.Cells[1, 3].Value = "ExpectedExplanation";
                worksheet.Cells[1, 4].Value = "ComplexityLevel";
                worksheet.Cells[1, 5].Value = "QueryCategory";
                worksheet.Cells[1, 6].Value = "ExpectedRowCount";
                worksheet.Cells[1, 7].Value = "GeneratedSQL";
                worksheet.Cells[1, 8].Value = "ActualSQL"; // New column
                worksheet.Cells[1, 9].Value = "GeneratedExplanation";
                worksheet.Cells[1, 10].Value = "ActualRowCount";
                worksheet.Cells[1, 11].Value = "DataMatchScore";
                worksheet.Cells[1, 12].Value = "ResultMatchStatus";
                worksheet.Cells[1, 13].Value = "ExecutionTimeMs";
                worksheet.Cells[1, 14].Value = "LLMUsed";
                worksheet.Cells[1, 15].Value = "Status";
                worksheet.Cells[1, 16].Value = "ErrorMessage";

                // Format headers
                using (var range = worksheet.Cells[1, 1, 1, 16])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // Add test cases data
                for (int i = 0; i < request.TestCases.Count; i++)
                {
                    var testCase = request.TestCases[i];
                    int row = i + 2; // Start from row 2 (after header)

                    worksheet.Cells[row, 1].Value = testCase.Question;
                    worksheet.Cells[row, 2].Value = testCase.ExpectedSql;
                    worksheet.Cells[row, 3].Value = testCase.ExpectedExplanation;
                    worksheet.Cells[row, 4].Value = testCase.ComplexityLevel;
                    worksheet.Cells[row, 5].Value = testCase.QueryCategory;
                    // ExpectedRowCount in column 6 will be calculated during processing
                }

                // Add dropdown lists for complexity level and query category
                var complexityValidation = worksheet.DataValidations.AddListValidation("D2:D1000");
                complexityValidation.Formula.Values.Add("Simple");
                complexityValidation.Formula.Values.Add("Medium");
                complexityValidation.Formula.Values.Add("Complex");
                complexityValidation.Formula.Values.Add("Very Complex");

                var categoryValidation = worksheet.DataValidations.AddListValidation("E2:E1000");
                categoryValidation.Formula.Values.Add("Aggregate");
                categoryValidation.Formula.Values.Add("Filter");
                categoryValidation.Formula.Values.Add("Join");
                categoryValidation.Formula.Values.Add("GroupBy");
                categoryValidation.Formula.Values.Add("OrderBy");
                categoryValidation.Formula.Values.Add("SubQuery");
                categoryValidation.Formula.Values.Add("CTE");
                categoryValidation.Formula.Values.Add("Complex");

                // Set column widths - Updated column layout
                worksheet.Column(1).Width = 50;  // Question
                worksheet.Column(2).Width = 70;  // ExpectedSQL
                worksheet.Column(3).Width = 50;  // ExpectedExplanation
                worksheet.Column(4).Width = 15;  // ComplexityLevel
                worksheet.Column(5).Width = 15;  // QueryCategory
                worksheet.Column(6).Width = 15;  // ExpectedRowCount
                worksheet.Column(7).Width = 70;  // GeneratedSQL
                worksheet.Column(8).Width = 70;  // ActualSQL
                worksheet.Column(9).Width = 50;  // GeneratedExplanation
                worksheet.Column(10).Width = 15; // ActualRowCount
                worksheet.Column(11).Width = 15; // DataMatchScore
                worksheet.Column(12).Width = 20; // ResultMatchStatus
                worksheet.Column(13).Width = 15; // ExecutionTimeMs
                worksheet.Column(14).Width = 15; // LLMUsed
                worksheet.Column(15).Width = 15; // Status
                worksheet.Column(16).Width = 50; // ErrorMessage

                // Protect cells that should not be edited (system-filled columns)
                worksheet.Cells[$"G2:P1000"].Style.Locked = true;

                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                string errorDetails = GetDetailedExceptionInfo(ex);
                throw new Exception($"Error converting JSON to Excel template: {ex.Message} | {errorDetails}", ex);
            }
        }

        /// <summary>
        /// Retrieves job details with pagination and total count.
        /// </summary>
        /// <param name="jobId">The job ID.</param>
        /// <param name="pageNumber">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns>Tuple containing paginated job details and total count.</returns>
        public async Task<(IEnumerable<TestAutomationDetail> Data, int TotalCount)> GetJobDetailsPaginatedAsync(
            int jobId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                return await objTestAutomationRepostiroy.GetTestDetailsPaginatedAsync(jobId, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                string errorDetails = GetDetailedExceptionInfo(ex);
                throw new Exception($"Error retrieving test job details: {ex.Message} | {errorDetails}", ex);
            }
        }

        /// <summary>
        /// Retrieves dataset comparison data for a specific test detail.
        /// </summary>
        /// <param name="detailId">The ID of the test detail.</param>
        /// <returns>A comprehensive dataset comparison result.</returns>
        public async Task<DatasetComparisonResult> GetDatasetComparisonAsync(int detailId)
        {
            try
            {
                // Retrieve the datasets from the repository
                var expectedDataset = await objTestAutomationRepostiroy.GetDatasetAsync(detailId, true);
                var actualDataset = await objTestAutomationRepostiroy.GetDatasetAsync(detailId, false);

                List<Dictionary<string, object>> expectedData = new List<Dictionary<string, object>>();
                List<Dictionary<string, object>> actualData = new List<Dictionary<string, object>>();

                // Deserialize expected dataset if available
                if (expectedDataset != null && !string.IsNullOrWhiteSpace(expectedDataset.DatasetJSON))
                {
                    try
                    {
                        // Use more tolerant deserialization settings
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                        };

                        expectedData = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(
                            expectedDataset.DatasetJSON, options);
                    }
                    catch (Exception ex)
                    {
                        string errorDetails = GetDetailedExceptionInfo(ex);
                        Console.Error.WriteLine($"Error deserializing expected dataset: {ex.Message} | {errorDetails}");
                        // Create an empty dataset if deserialization fails
                        expectedData = new List<Dictionary<string, object>>();
                    }
                }

                // Deserialize actual dataset if available
                if (actualDataset != null && !string.IsNullOrWhiteSpace(actualDataset.DatasetJSON))
                {
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                        };

                        actualData = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(
                            actualDataset.DatasetJSON, options);
                    }
                    catch (Exception ex)
                    {
                        string errorDetails = GetDetailedExceptionInfo(ex);
                        Console.Error.WriteLine($"Error deserializing actual dataset: {ex.Message} | {errorDetails}");
                        // Create an empty dataset if deserialization fails
                        actualData = new List<Dictionary<string, object>>();
                    }
                }

                // Get the test detail to add additional information
                var testDetail = await objTestAutomationRepostiroy.GetTestDetailByIdAsync(detailId);

                // Use the dataset comparison service to compare the datasets
                var comparisonResult = _datasetComparisonService.CompareDatasets(expectedData, actualData);

                // Add metadata from the test detail
                if (testDetail != null)
                {
                    comparisonResult.TestDetail = new TestDetailMetadata
                    {
                        DetailID = testDetail.DetailID,
                        Question = testDetail.Question,
                        ExpectedRowCount = testDetail.ExpectedRowCount,
                        ActualRowCount = testDetail.ActualRowCount,
                        DataMatchScore = testDetail.DataMatchScore,
                        ResultMatchStatus = testDetail.ResultMatchStatus,
                        Success = testDetail.Success
                    };
                }

                return comparisonResult;
            }
            catch (Exception ex)
            {
                // Log the error and return an empty comparison result
                string errorDetails = GetDetailedExceptionInfo(ex);
                Console.Error.WriteLine($"Error retrieving dataset comparison: {ex.Message} | {errorDetails}");
                return new DatasetComparisonResult
                {
                    ComparisonSummary = $"Error retrieving dataset comparison: {ex.Message} | {errorDetails}",
                    IsEquivalent = false
                };
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Normalizes dataset for consistent storage.
        /// </summary>
        private List<Dictionary<string, object>> NormalizeDatasetForStorage(List<Dictionary<string, object>> dataset)
        {
            if (dataset == null || !dataset.Any())
                return new List<Dictionary<string, object>>();

            // Create a normalized copy of the dataset
            var normalized = new List<Dictionary<string, object>>();

            // Get a canonical list of all column names to ensure consistency
            var allColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in dataset)
            {
                foreach (var key in row.Keys)
                {
                    allColumns.Add(key);
                }
            }

            // Sort column names alphabetically for consistency
            var orderedColumns = allColumns.OrderBy(c => c).ToList();

            // Create normalized rows with all columns
            foreach (var row in dataset)
            {
                var normalizedRow = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                // Ensure each row has all columns, with nulls for missing values
                foreach (var column in orderedColumns)
                {
                    string actualColumn = row.Keys.FirstOrDefault(k =>
                        string.Equals(k, column, StringComparison.OrdinalIgnoreCase));

                    if (actualColumn != null && row.TryGetValue(actualColumn, out var value))
                    {
                        // Normalize value based on type
                        if (value is DateTime dateTime)
                        {
                            normalizedRow[column] = dateTime.ToString("o"); // ISO 8601 format
                        }
                        else if (value is DBNull)
                        {
                            normalizedRow[column] = null;
                        }
                        else if (value is JsonElement jsonElement)
                        {
                            // Handle different JsonElement types
                            switch (jsonElement.ValueKind)
                            {
                                case JsonValueKind.Null:
                                    normalizedRow[column] = null;
                                    break;
                                case JsonValueKind.Number:
                                    if (jsonElement.TryGetInt32(out var intValue))
                                        normalizedRow[column] = intValue;
                                    else if (jsonElement.TryGetDouble(out var doubleValue))
                                        normalizedRow[column] = doubleValue;
                                    else if (jsonElement.TryGetDecimal(out var decimalValue))
                                        normalizedRow[column] = decimalValue;
                                    else
                                        normalizedRow[column] = jsonElement.GetRawText();
                                    break;
                                case JsonValueKind.String:
                                    normalizedRow[column] = jsonElement.GetString();
                                    break;
                                case JsonValueKind.True:
                                    normalizedRow[column] = true;
                                    break;
                                case JsonValueKind.False:
                                    normalizedRow[column] = false;
                                    break;
                                default:
                                    normalizedRow[column] = jsonElement.GetRawText();
                                    break;
                            }
                        }
                        else
                        {
                            normalizedRow[column] = value;
                        }
                    }
                    else
                    {
                        normalizedRow[column] = null;
                    }
                }

                normalized.Add(normalizedRow);
            }

            return normalized;
        }

        /// <summary>
        /// Validates that the worksheet has the expected structure for test automation.
        /// </summary>
        /// <param name="worksheet">The worksheet to validate.</param>
        private void ValidateWorksheetStructure(OfficeOpenXml.ExcelWorksheet worksheet)
        {
            if (worksheet.Dimension == null || worksheet.Dimension.Rows < 2)
            {
                throw new ArgumentException("The uploaded file does not contain valid test data. Please use the template format.");
            }

            // Check for required header columns
            var requiredHeaders = new[] { "Question", "ExpectedSQL", "ExpectedExplanation" };
            for (int i = 0; i < requiredHeaders.Length; i++)
            {
                var headerText = worksheet.Cells[1, i + 1].Text;
                if (!string.Equals(headerText, requiredHeaders[i], StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException($"Invalid template format. Expected header '{requiredHeaders[i]}' at column {i + 1}, found '{headerText}'.");
                }
            }
        }

        /// <summary>
        /// Finds the last row with data in the worksheet.
        /// </summary>
        private int FindLastDataRow(ExcelWorksheet worksheet)
        {
            int lastRow = 1; // Start with header row

            // Find the last row with data
            for (int row = worksheet.Dimension?.Rows ?? 0; row >= 2; row--)
            {
                // Check if this row has any data in the first few columns
                bool hasData = false;
                for (int col = 1; col <= Math.Min(5, worksheet.Dimension?.Columns ?? 0); col++)
                {
                    if (!string.IsNullOrWhiteSpace(worksheet.Cells[row, col].Text))
                    {
                        hasData = true;
                        break;
                    }
                }

                if (hasData)
                {
                    lastRow = row;
                    break;
                }
            }

            return lastRow;
        }

        /// <summary>
        /// Formats the Excel output after processing.
        /// </summary>
        /// <param name="worksheet">The worksheet to format.</param>
        /// <param name="rowCount">The number of rows to format.</param>
        private void FormatExcelOutput(OfficeOpenXml.ExcelWorksheet worksheet, int rowCount)
        {
            // Format all rows that were processed
            for (int row = 2; row <= rowCount; row++)
            {
                // Format success/failure rows differently
                string status = worksheet.Cells[row, 15].Text; // Updated status column index
                if (string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase))
                {
                    worksheet.Cells[row, 15].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[row, 15].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                }
                else if (string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(status, "Error", StringComparison.OrdinalIgnoreCase))
                {
                    worksheet.Cells[row, 15].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[row, 15].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightPink);
                }

                // Format match scores with conditional formatting and percentage display
                FormatMatchScoreCell(worksheet.Cells[row, 11]); // Data match score - Updated column index
            }
        }

        /// <summary>
        /// Formats a match score cell with color coding.
        /// </summary>
        /// <param name="cell">The cell to format.</param>
        private void FormatMatchScoreCell(OfficeOpenXml.ExcelRange cell)
        {
            if (cell.Value == null || !(cell.Value is decimal || cell.Value is double))
                return;

            // Parse the value
            decimal score;
            if (cell.Value is decimal)
                score = (decimal)cell.Value;
            else if (cell.Value is double)
                score = (decimal)(double)cell.Value;
            else
                return;

            // Format with percentage and color
            cell.Style.Numberformat.Format = "0.00%";
            cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;

            // Color coding based on score ranges
            if (score >= 0.9m)
                cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
            else if (score >= 0.7m)
                cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
            else
                cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightPink);
        }

        /// <summary>
        /// Returns 50 test questions as specified in the business document.
        /// </summary>
        /// <returns>Array of sample questions.</returns>
        private string[] Get50TestQuestions()
        {
            return new string[]
            {
                "Show me the top 10 customers by total order value in the last 6 months",
                "Which products have less than 10 items in stock and need to be reordered?",
                "Calculate the average order value grouped by customer country",
                "List all orders that contain products from at least 3 different categories",
                "Show me the customer who has spent the most in each product category",
                "Which employees have processed more than 100 orders in Q1 2023?",
                "Find all products that have been returned more than 5 times in the past year",
                "What is the monthly revenue trend for the last 12 months?",
                "Show me customers who have placed orders in consecutive months",
                "Identify products that have never been ordered",
                "Calculate the profit margin for each product category",
                "List customers who have spent more than $5000 but haven't made a purchase in the last 3 months",
                "Rank warehouses by available inventory value",
                "Which marketing campaigns had an ROI greater than 200%?",
                "Find all customers who purchased Product X but not Product Y",
                "What percentage of orders are shipped within 24 hours of being placed?",
                "Show me the top 5 most frequently purchased product pairs",
                "Calculate the average customer lifetime value by registration month",
                "Identify products whose sales have increased by more than 20% month-over-month",
                "List employees who have never processed an order with a return",
                "What is the distribution of order values across different payment methods?",
                "Find customers whose average order value has decreased in the last 3 months",
                "Which products have the highest variance in order quantity?",
                "Show me the suppliers with the longest average lead time for product delivery",
                "List all products with price higher than the average price in their category",
                "Find customers who have written reviews for products they haven't purchased",
                "What's the correlation between product price and customer rating?",
                "Identify products that are frequently purchased together with promotional items",
                "Calculate the percentage of customers who made a second purchase within 30 days of their first order",
                "Show me employees with the highest average order value per transaction",
                "List all customers who have used all available payment methods",
                "Which product categories have the highest customer retention rate?",
                "Find orders where the shipping cost is more than 15% of the order value",
                "Identify customers who consistently place orders on the same day of the week",
                "Show me the most popular products by age group (based on customer birth date)",
                "Calculate the average time between consecutive orders for each customer",
                "Which promotional campaigns resulted in the highest new customer acquisition?",
                "List all products where the inventory turnover rate is less than the category average",
                "Find customers who have complained about shipping delays but have never returned a product",
                "What is the trend of average product rating over time?",
                "Show me products that are often purchased as gifts (based on different shipping and billing addresses)",
                "Identify customers whose purchasing behavior changed significantly after a specific marketing campaign",
                "Calculate the optimal reorder point for each product based on historical sales and lead time",
                "List products that have been viewed more than 100 times but purchased less than 5 times",
                "Find all orders where the customer spent more than their average order value",
                "Show me product categories with the highest seasonal variation in sales",
                "Calculate the customer churn rate by month for the past year",
                "Identify employees whose sales performance has consistently improved quarter over quarter",
                "Which shipping carriers have the lowest rate of delivery delays?",
                "Find customers who have spent more than the average in each product category they've purchased from"
            };
        }

        #endregion
    }
}