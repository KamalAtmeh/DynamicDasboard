using DynamicDasboardWebAPI.Repositories;
using DynamicDasboardWebAPI.Repositories.TestAutomation;
using DynamicDasboardWebAPI.Services.LLM;
using DynamicDasboardWebAPI.Utilities;
using DynamicDashboardCommon.Models;
using DynamicDashboardCommon.Models.TestAutomation;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly QueryRepository objQueryRepository;
        private readonly LLMServiceFactory objLLMServiceFactory;
        private readonly DatabaseService objDataBaseService;
        private readonly DatabaseSchemaService objDatabaseSchemaService;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestAutomationService"/> class.
        /// </summary>
        /// <param name="testRepository">Repository for test automation data.</param>
        /// <param name="queryRepository">Repository for executing queries.</param>
        /// <param name="llmServiceFactory">Factory for creating LLM service instances.</param>
        /// <param name="databaseService">Service for database operations.</param>
        /// <param name="databaseSchema"></param>
        public TestAutomationService(
            TestAutomationRepository testRepository,
            QueryRepository queryRepository,
            LLMServiceFactory llmServiceFactory,
            DatabaseService databaseService, DatabaseSchemaService databaseSchemaService)
        {
            objTestAutomationRepostiroy = testRepository ?? throw new ArgumentNullException(nameof(testRepository));
            objQueryRepository = queryRepository ?? throw new ArgumentNullException(nameof(queryRepository));
            objLLMServiceFactory = llmServiceFactory ?? throw new ArgumentNullException(nameof(llmServiceFactory));
            objDataBaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            objDatabaseSchemaService = databaseSchemaService ?? throw new ArgumentNullException(nameof(objDatabaseSchemaService));
            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        /// <summary>
        /// Processes a test cases file and runs the test automation.
        /// </summary>
        /// <param name="fileStream">The input file stream containing test cases.</param>
        /// <param name="databaseId">The ID of the database schema to test against.</param>
        /// <param name="llmProvider">The LLM provider to use for testing.</param>
        /// <param name="userId">The user ID of the person running the test (optional).</param>
        /// <returns>The processed file with test results as a byte array.</returns>
        public async Task<byte[]> ProcessTestCasesFileAsync(Stream fileStream, int databaseId, string llmProvider, int? userId = null)
        {
            try
            {
                // Initialize Excel package
                using var package = new ExcelPackage(fileStream);
                var worksheet = package.Workbook.Worksheets[0];

                // Check if the worksheet has the expected structure
                ValidateWorksheetStructure(worksheet);

                // Define column indices based on updated structure
                const int questionCol = 1;          // Column A
                const int expectedSqlCol = 2;       // Column B
                const int expectedExplanationCol = 3; // Column C
                const int complexityLevelCol = 4;   // Column D
                const int queryCategoryCol = 5;     // Column E
                const int expectedRowCountCol = 6;  // Column F

                // Output columns
                const int generatedSqlCol = 7;      // Column G
                const int generatedExplanationCol = 8; // Column H
                const int sqlMatchScoreCol = 9;     // Column I
                const int explanationMatchScoreCol = 10; // Column J
                const int actualRowCountCol = 11;   // Column K
                const int dataMatchScoreCol = 12;   // Column L
                const int resultMatchStatusCol = 13; // Column M
                const int executionTimeCol = 14;    // Column N
                const int llmUsedCol = 15;          // Column O
                const int statusCol = 16;           // Column P
                const int errorCol = 17;            // Column Q

                // Initialize tracking variables
                int lastDataRow = 1; // Start with header row

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
                        lastDataRow = row;
                        break;
                    }
                }
                int rowCount = lastDataRow;

                int totalQuestions = 0;
                int successCount = 0;
                decimal totalSqlMatchScore = 0;
                decimal totalExplanationMatchScore = 0;
                decimal totalDataMatchScore = 0;

                string fileName = "TestAutomation_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx";

                // CRITICAL FIX: Create a test job record first and use the returned ID consistently
                int jobId = await objTestAutomationRepostiroy.LogTestJobAsync(
                    fileName,
                    databaseId,
                    0, // Initialize with 0, will update later
                    0, // Initialize with 0, will update later
                    llmProvider,
                    0, // Average scores - will update later
                    0,
                    0,
                    userId
                );

                // Get LLM service instance
                var llmService = objLLMServiceFactory.CreateLlmService();

                // Get database info for schema
                var database = await objDataBaseService.GetDatabaseByIdAsync(databaseId);
                if (database == null)
                {
                    throw new ArgumentException($"Database with ID {databaseId} not found");
                }

                // Get database schema for LLM context
                var objDBSchema = await objDatabaseSchemaService.GetSchemaObject(database.DatabaseID);
                if (objDBSchema == null)
                {
                    throw new ArgumentException("Could not retrieve database schema");
                }

                var schemaStr = objDatabaseSchemaService.BuildOptimizedSchemaString(objDBSchema);

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

                        // Step 2: Compare the generated SQL with the expected SQL
                        decimal sqlMatchScore = ComparisonUtilities.GetSQLSimilarity(expectedSql, generatedSql);

                        // Step 3: Compare the generated explanation with the expected explanation
                        decimal explanationMatchScore = ComparisonUtilities.GetExplanationSimilarity(
                            expectedExplanation,
                            generatedExplanation);

                        // Step 4: Execute the expected SQL query if provided
                        List<Dictionary<string, object>> expectedDataset = null;
                        int? expectedRowCount = null;
                        if (!string.IsNullOrWhiteSpace(expectedSql))
                        {
                            try
                            {
                                expectedDataset = await objQueryRepository.ExecuteQueryOnDatabaseAsync(expectedSql, databaseId);
                                expectedRowCount = expectedDataset?.Count; // Set the expected row count from actual execution
                                worksheet.Cells[row, expectedRowCountCol].Value = expectedRowCount;
                            }
                            catch (Exception ex)
                            {
                                // If expected SQL fails, log it but continue
                                worksheet.Cells[row, errorCol].Value = $"Expected SQL execution failed: {ex.Message}";
                            }
                        }

                        // Step 5: Execute the generated SQL query
                        List<Dictionary<string, object>> actualDataset = null;
                        int? actualRowCount = null;
                        if (!string.IsNullOrWhiteSpace(generatedSql))
                        {
                            try
                            {
                                actualDataset = await objQueryRepository.ExecuteQueryOnDatabaseAsync(generatedSql, databaseId);
                                actualRowCount = actualDataset?.Count;
                            }
                            catch (Exception ex)
                            {
                                worksheet.Cells[row, errorCol].Value = $"Generated SQL execution failed: {ex.Message}";
                            }
                        }

                        // Step 6: Compare datasets and compute similarity score
                        decimal dataMatchScore = 0;
                        string resultMatchStatus = "Not Compared";

                        if (expectedDataset != null && actualDataset != null)
                        {
                            var (score, status) = ComparisonUtilities.GetDatasetSimilarity(expectedDataset, actualDataset);
                            dataMatchScore = score;
                            resultMatchStatus = status;
                        }

                        // Calculate execution time
                        var endTime = DateTime.Now;
                        var executionTimeMs = (int)(endTime - startTime).TotalMilliseconds;

                        // Step 7: Create test detail record in database
                        // CRITICAL FIX: Use the jobId obtained earlier
                        detailId = await objTestAutomationRepostiroy.LogTestDetailAsync(
                            jobId, // Use the jobId from the first creation
                            question,
                            expectedSql,
                            generatedSql,
                            sqlMatchScore,
                            expectedExplanation,
                            generatedExplanation,
                            explanationMatchScore,
                            expectedRowCount,  // Set from actual execution
                            actualRowCount,
                            dataMatchScore,
                            resultMatchStatus,
                            complexityLevel,
                            queryCategory,
                            executionTimeMs,
                            true, // Success
                            null  // No error
                        );

                        // Step 8: Store datasets for detailed comparison
                        if (expectedDataset != null)
                        {
                            await StoreDatasetAsync(detailId, true, expectedDataset);
                        }

                        if (actualDataset != null)
                        {
                            await StoreDatasetAsync(detailId, false, actualDataset);
                        }

                        // Step 9: Update Excel with results
                        worksheet.Cells[row, generatedSqlCol].Value = generatedSql;
                        worksheet.Cells[row, generatedExplanationCol].Value = generatedExplanation;
                        worksheet.Cells[row, sqlMatchScoreCol].Value = sqlMatchScore;
                        worksheet.Cells[row, explanationMatchScoreCol].Value = explanationMatchScore;
                        worksheet.Cells[row, actualRowCountCol].Value = actualRowCount;
                        worksheet.Cells[row, dataMatchScoreCol].Value = dataMatchScore;
                        worksheet.Cells[row, resultMatchStatusCol].Value = resultMatchStatus;
                        worksheet.Cells[row, executionTimeCol].Value = executionTimeMs;
                        worksheet.Cells[row, llmUsedCol].Value = llmProvider;
                        worksheet.Cells[row, statusCol].Value = "Success";

                        // Update running totals
                        successCount++;
                        totalSqlMatchScore += sqlMatchScore;
                        totalExplanationMatchScore += explanationMatchScore;
                        totalDataMatchScore += dataMatchScore;
                    }
                    catch (Exception ex)
                    {
                        // Handle errors and log them
                        worksheet.Cells[row, statusCol].Value = "Error";
                        worksheet.Cells[row, errorCol].Value = ex.Message;

                        // Log error to database if we haven't created a detail record yet
                        if (detailId == 0)
                        {
                            // CRITICAL FIX: Use the jobId obtained earlier
                            await objTestAutomationRepostiroy.LogTestDetailAsync(
                                jobId, // Use the same jobId
                                question,
                                worksheet.Cells[row, expectedSqlCol].Text, // Expected SQL
                                null, // Generated SQL
                                null, // SQL match score
                                worksheet.Cells[row, expectedExplanationCol].Text, // Expected explanation
                                null, // Generated explanation
                                null, // Explanation match score
                                null, // Expected row count
                                null, // Actual row count
                                null, // Data match score
                                "Error", // Result match status
                                worksheet.Cells[row, complexityLevelCol].Text, // Complexity level
                                worksheet.Cells[row, queryCategoryCol].Text, // Query category
                                null, // Execution time
                                false, // Success flag
                                ex.Message // Error message
                            );
                        }
                    }
                }

                // Update the job with final counts and averages
                decimal avgSqlScore = successCount > 0 ? totalSqlMatchScore / successCount : 0;
                decimal avgExplanationScore = successCount > 0 ? totalExplanationMatchScore / successCount : 0;
                decimal avgDataScore = successCount > 0 ? totalDataMatchScore / successCount : 0;

                // CRITICAL FIX: Update the existing job record instead of creating a new one
                await objTestAutomationRepostiroy.UpdateTestJobAsync(
                    jobId, // Use the same jobId
                    totalQuestions,
                    successCount,
                    avgSqlScore,
                    avgExplanationScore,
                    avgDataScore
                );

                // Format the Excel file
                FormatExcelOutput(worksheet, rowCount);

                // Return the modified Excel file
                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing test cases: {ex.Message}", ex);
            }
        }

        public byte[] GenerateTestTemplate()
        {
            try
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Test Cases");

                // Define headers according to the business document
                worksheet.Cells[1, 1].Value = "Question";
                worksheet.Cells[1, 2].Value = "ExpectedSQL";
                worksheet.Cells[1, 3].Value = "ExpectedExplanation";
                worksheet.Cells[1, 4].Value = "ComplexityLevel";
                worksheet.Cells[1, 5].Value = "QueryCategory";
                worksheet.Cells[1, 6].Value = "ExpectedRowCount";
                worksheet.Cells[1, 7].Value = "GeneratedSQL";
                worksheet.Cells[1, 8].Value = "GeneratedExplanation";
                worksheet.Cells[1, 9].Value = "SQLMatchScore";
                worksheet.Cells[1, 10].Value = "ExplanationMatchScore";
                worksheet.Cells[1, 11].Value = "ActualRowCount";
                worksheet.Cells[1, 12].Value = "DataMatchScore";
                worksheet.Cells[1, 13].Value = "ResultMatchStatus";
                worksheet.Cells[1, 14].Value = "ExecutionTimeMs";
                worksheet.Cells[1, 15].Value = "LLMUsed";
                worksheet.Cells[1, 16].Value = "Status";
                worksheet.Cells[1, 17].Value = "ErrorMessage";

                // Format headers
                using (var range = worksheet.Cells[1, 1, 1, 17])
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

                // Set column widths
                worksheet.Column(1).Width = 40;  // Question
                worksheet.Column(2).Width = 50;  // ExpectedSQL
                worksheet.Column(3).Width = 50;  // ExpectedExplanation
                worksheet.Column(4).Width = 15;  // ComplexityLevel
                worksheet.Column(5).Width = 15;  // QueryCategory
                worksheet.Column(6).Width = 15;  // ExpectedRowCount
                worksheet.Column(7).Width = 50;  // GeneratedSQL
                worksheet.Column(8).Width = 50;  // GeneratedExplanation
                worksheet.Column(9).Width = 15;  // SQLMatchScore
                worksheet.Column(10).Width = 15; // ExplanationMatchScore
                worksheet.Column(11).Width = 15; // ActualRowCount
                worksheet.Column(12).Width = 15; // DataMatchScore
                worksheet.Column(13).Width = 20; // ResultMatchStatus
                worksheet.Column(14).Width = 15; // ExecutionTimeMs
                worksheet.Column(15).Width = 15; // LLMUsed
                worksheet.Column(16).Width = 15; // Status
                worksheet.Column(17).Width = 50; // ErrorMessage

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
                worksheet.Cells[$"G3:Q1000"].Style.Locked = true;

                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating test template: {ex.Message}", ex);
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

                // Add headers
                worksheet.Cells[1, 1].Value = "Question";
                worksheet.Cells[1, 2].Value = "ExpectedSQL";
                worksheet.Cells[1, 3].Value = "ExpectedExplanation";
                worksheet.Cells[1, 4].Value = "ComplexityLevel";
                worksheet.Cells[1, 5].Value = "QueryCategory";
                worksheet.Cells[1, 6].Value = "ExpectedRowCount";
                worksheet.Cells[1, 7].Value = "GeneratedSQL";
                worksheet.Cells[1, 8].Value = "GeneratedExplanation";
                worksheet.Cells[1, 9].Value = "SQLMatchScore";
                worksheet.Cells[1, 10].Value = "ExplanationMatchScore";
                worksheet.Cells[1, 11].Value = "ActualRowCount";
                worksheet.Cells[1, 12].Value = "DataMatchScore";
                worksheet.Cells[1, 13].Value = "ResultMatchStatus";
                worksheet.Cells[1, 14].Value = "ExecutionTimeMs";
                worksheet.Cells[1, 15].Value = "LLMUsed";
                worksheet.Cells[1, 16].Value = "Status";
                worksheet.Cells[1, 17].Value = "ErrorMessage";

                // Format headers
                using (var range = worksheet.Cells[1, 1, 1, 17])
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

                // Set column widths
                worksheet.Column(1).Width = 50;  // Question
                worksheet.Column(2).Width = 70;  // ExpectedSQL
                worksheet.Column(3).Width = 50;  // ExpectedExplanation
                worksheet.Column(4).Width = 15;  // ComplexityLevel
                worksheet.Column(5).Width = 15;  // QueryCategory
                worksheet.Column(6).Width = 15;  // ExpectedRowCount
                worksheet.Column(7).Width = 70;  // GeneratedSQL
                worksheet.Column(8).Width = 50;  // GeneratedExplanation
                worksheet.Column(9).Width = 15;  // SQLMatchScore
                worksheet.Column(10).Width = 15; // ExplanationMatchScore
                worksheet.Column(11).Width = 15; // ActualRowCount
                worksheet.Column(12).Width = 15; // DataMatchScore
                worksheet.Column(13).Width = 20; // ResultMatchStatus
                worksheet.Column(14).Width = 15; // ExecutionTimeMs
                worksheet.Column(15).Width = 15; // LLMUsed
                worksheet.Column(16).Width = 15; // Status
                worksheet.Column(17).Width = 30; // ErrorMessage

                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating sample test questions: {ex.Message}", ex);
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
                throw new Exception($"Error retrieving recent test jobs: {ex.Message}", ex);
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
                throw new Exception($"Error retrieving test job details: {ex.Message}", ex);
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

                // Add headers according to the template (removing ExpectedRowCount)
                worksheet.Cells[1, 1].Value = "Question";
                worksheet.Cells[1, 2].Value = "ExpectedSQL";
                worksheet.Cells[1, 3].Value = "ExpectedExplanation";
                worksheet.Cells[1, 4].Value = "ComplexityLevel";
                worksheet.Cells[1, 5].Value = "QueryCategory";
                worksheet.Cells[1, 6].Value = "GeneratedSQL";
                worksheet.Cells[1, 7].Value = "GeneratedExplanation";
                worksheet.Cells[1, 8].Value = "SQLMatchScore";
                worksheet.Cells[1, 9].Value = "ExplanationMatchScore";
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
                    worksheet.Cells[1, 6].Value = "ExpectedRowCount";
                    // Columns 6-16 are left empty as they'll be filled by the system during test execution
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

                // Set column widths
                worksheet.Column(1).Width = 50;  // Question
                worksheet.Column(2).Width = 70;  // ExpectedSQL
                worksheet.Column(3).Width = 50;  // ExpectedExplanation
                worksheet.Column(4).Width = 15;  // ComplexityLevel
                worksheet.Column(5).Width = 15;  // QueryCategory
                worksheet.Column(6).Width = 70;  // GeneratedSQL
                worksheet.Column(7).Width = 50;  // GeneratedExplanation
                worksheet.Column(8).Width = 15;  // SQLMatchScore
                worksheet.Column(9).Width = 15;  // ExplanationMatchScore
                worksheet.Column(10).Width = 15; // ActualRowCount
                worksheet.Column(11).Width = 15; // DataMatchScore
                worksheet.Column(12).Width = 20; // ResultMatchStatus
                worksheet.Column(13).Width = 15; // ExecutionTimeMs
                worksheet.Column(14).Width = 15; // LLMUsed
                worksheet.Column(15).Width = 15; // Status
                worksheet.Column(16).Width = 50; // ErrorMessage

                // Protect cells that should not be edited (system-filled columns)
                worksheet.Cells[$"F2:P1000"].Style.Locked = true;

                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error converting JSON to Excel template: {ex.Message}", ex);
            }
        }
    

        // Add to DynamicDasboardWebAPI/Services/TestAutomation/TestAutomationService.cs

        /// <summary>
        /// Retrieves job details with pagination.
        /// </summary>
        /// <param name="jobId">The job ID.</param>
        /// <param name="pageNumber">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns>Paginated job details.</returns>
        public async Task<IEnumerable<TestAutomationDetail>> GetJobDetailsPaginatedAsync(
            int jobId, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                return await objTestAutomationRepostiroy.GetTestDetailsPaginatedAsync(jobId, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving test job details: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves dataset comparison data for a specific test detail.
        /// </summary>
        /// <param name="detailId">The ID of the test detail.</param>
        /// <returns>A tuple containing the expected and actual datasets.</returns>
        public async Task<DatasetComparisonResult> GetDatasetComparisonAsync(int detailId)
        {
            try
            {
                var expectedDataset = await objTestAutomationRepostiroy.GetDatasetAsync(detailId, true);
                var actualDataset = await objTestAutomationRepostiroy.GetDatasetAsync(detailId, false);

                List<Dictionary<string, object>> expectedData = null;
                List<Dictionary<string, object>> actualData = null;

                // Deserialize expected dataset if available
                if (expectedDataset != null && !string.IsNullOrWhiteSpace(expectedDataset.DatasetJSON))
                {
                    try
                    {
                        // Use a more tolerant JSON deserializer to handle property value conversion
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                        };

                        expectedData = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(
                            expectedDataset.DatasetJSON, options);

                        // Ensure proper type conversion for numeric values
                        if (expectedData != null)
                        {
                            expectedData = NormalizeDatasetTypes(expectedData);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error deserializing expected dataset: {ex.Message}");
                        // Provide a fallback if deserialization fails
                        expectedData = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object> { { "Error", "Failed to parse expected dataset" } }
                };
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

                        // Ensure proper type conversion for numeric values
                        if (actualData != null)
                        {
                            actualData = NormalizeDatasetTypes(actualData);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error deserializing actual dataset: {ex.Message}");
                        // Provide a fallback if deserialization fails
                        actualData = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object> { { "Error", "Failed to parse actual dataset" } }
                };
                    }
                }

                // Create and return the DatasetComparisonResult object
                return new DatasetComparisonResult
                {
                    Expected = expectedData ?? new List<Dictionary<string, object>>(),
                    Actual = actualData ?? new List<Dictionary<string, object>>()
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving dataset comparison: {ex.Message}", ex);
            }
        }



        #region Private Helper Methods

        /// <summary>
        /// Normalizes data types in a dataset to ensure proper comparison.
        /// JSON deserialization can sometimes produce inconsistent types.
        /// </summary>
        private List<Dictionary<string, object>> NormalizeDatasetTypes(List<Dictionary<string, object>> dataset)
        {
            if (dataset == null || dataset.Count == 0) return dataset;

            var result = new List<Dictionary<string, object>>();

            // First, determine the most appropriate type for each column
            var columnTypes = new Dictionary<string, Type>();
            foreach (var row in dataset)
            {
                foreach (var kvp in row)
                {
                    if (kvp.Value != null)
                    {
                        if (!columnTypes.TryGetValue(kvp.Key, out var currentType))
                        {
                            columnTypes[kvp.Key] = kvp.Value.GetType();
                        }
                        else if (kvp.Value.GetType() != currentType)
                        {
                            // If mixed types, prefer numeric types in this order: decimal, double, int
                            if (kvp.Value is decimal || currentType == typeof(decimal))
                            {
                                columnTypes[kvp.Key] = typeof(decimal);
                            }
                            else if (kvp.Value is double || currentType == typeof(double))
                            {
                                columnTypes[kvp.Key] = typeof(double);
                            }
                            else if (kvp.Value is int || currentType == typeof(int))
                            {
                                columnTypes[kvp.Key] = typeof(int);
                            }
                            // Default to string if mixed non-numeric types
                            else
                            {
                                columnTypes[kvp.Key] = typeof(string);
                            }
                        }
                    }
                }
            }

            // Now convert each value to the appropriate type
            foreach (var row in dataset)
            {
                var normalizedRow = new Dictionary<string, object>();

                foreach (var kvp in row)
                {
                    if (kvp.Value == null)
                    {
                        normalizedRow[kvp.Key] = null;
                        continue;
                    }

                    if (columnTypes.TryGetValue(kvp.Key, out var targetType))
                    {
                        try
                        {
                            if (targetType == typeof(decimal) && kvp.Value is JsonElement je)
                            {
                                if (je.TryGetDecimal(out var dec))
                                    normalizedRow[kvp.Key] = dec;
                                else if (je.TryGetDouble(out var dbl))
                                    normalizedRow[kvp.Key] = (decimal)dbl;
                                else if (je.TryGetInt32(out var i32))
                                    normalizedRow[kvp.Key] = (decimal)i32;
                                else
                                    normalizedRow[kvp.Key] = kvp.Value;
                            }
                            else if (targetType == typeof(double) && kvp.Value is JsonElement je2)
                            {
                                if (je2.TryGetDouble(out var dbl))
                                    normalizedRow[kvp.Key] = dbl;
                                else if (je2.TryGetInt32(out var i32))
                                    normalizedRow[kvp.Key] = (double)i32;
                                else
                                    normalizedRow[kvp.Key] = kvp.Value;
                            }
                            else if (targetType == typeof(int) && kvp.Value is JsonElement je3)
                            {
                                if (je3.TryGetInt32(out var i32))
                                    normalizedRow[kvp.Key] = i32;
                                else
                                    normalizedRow[kvp.Key] = kvp.Value;
                            }
                            else if (targetType == typeof(DateTime) && kvp.Value is JsonElement je4)
                            {
                                if (je4.TryGetDateTime(out var dt))
                                    normalizedRow[kvp.Key] = dt;
                                else
                                    normalizedRow[kvp.Key] = kvp.Value;
                            }
                            else if (kvp.Value is JsonElement je5)
                            {
                                // Handle other JsonElement cases
                                switch (je5.ValueKind)
                                {
                                    case JsonValueKind.String:
                                        normalizedRow[kvp.Key] = je5.GetString();
                                        break;
                                    case JsonValueKind.Number:
                                        if (je5.TryGetInt32(out var i))
                                            normalizedRow[kvp.Key] = i;
                                        else if (je5.TryGetDouble(out var d))
                                            normalizedRow[kvp.Key] = d;
                                        else if (je5.TryGetDecimal(out var dec))
                                            normalizedRow[kvp.Key] = dec;
                                        else
                                            normalizedRow[kvp.Key] = je5.GetRawText();
                                        break;
                                    case JsonValueKind.True:
                                        normalizedRow[kvp.Key] = true;
                                        break;
                                    case JsonValueKind.False:
                                        normalizedRow[kvp.Key] = false;
                                        break;
                                    case JsonValueKind.Null:
                                        normalizedRow[kvp.Key] = null;
                                        break;
                                    default:
                                        normalizedRow[kvp.Key] = je5.GetRawText();
                                        break;
                                }
                            }
                            else
                            {
                                // For regular objects, use the original value
                                normalizedRow[kvp.Key] = kvp.Value;
                            }
                        }
                        catch
                        {
                            // If conversion fails, use the original value
                            normalizedRow[kvp.Key] = kvp.Value;
                        }
                    }
                    else
                    {
                        normalizedRow[kvp.Key] = kvp.Value;
                    }
                }

                result.Add(normalizedRow);
            }

            return result;
        }

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

            // Check for required header columns as defined in the business document
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
        /// Stores a dataset for comparison in the database.
        /// </summary>
        /// <param name="detailId">The test detail ID.</param>
        /// <param name="isExpected">Whether this is the expected dataset.</param>
        /// <param name="dataset">The dataset to store.</param>
        /// <returns>The ID of the stored dataset.</returns>
        private async Task<int> StoreDatasetAsync(int detailId, bool isExpected, List<Dictionary<string, object>> dataset)
        {
            try
            {
                // Ensure the dataset is consistent before serialization
                var normalizedDataset = NormalizeDatasetForStorage(dataset);

                // Serialize the dataset to JSON with proper settings
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false, // Save space in the database
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // Handle special characters
                };

                string datasetJson = JsonSerializer.Serialize(normalizedDataset, options);

                // Get column names
                List<string> columnNames = normalizedDataset.Count > 0
                    ? normalizedDataset[0].Keys.ToList()
                    : new List<string>();

                string columnNamesJson = JsonSerializer.Serialize(columnNames, options);

                // Compute hash for quick comparison
                string dataHash = ComparisonUtilities.ComputeDatasetHash(normalizedDataset);

                // Store in database
                return await objTestAutomationRepostiroy.SaveDatasetAsync(
                    detailId,
                    isExpected,
                    datasetJson,
                    normalizedDataset.Count,
                    columnNames.Count,
                    columnNamesJson,
                    dataHash
                );
            }
            catch (Exception ex)
            {
                // Log error but continue - dataset storage is not critical to the overall process
                Console.Error.WriteLine($"Error storing dataset: {ex.Message}");
                return -1;
            }
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
                string status = worksheet.Cells[row, 16].Text;
                if (string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase))
                {
                    worksheet.Cells[row, 16].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[row, 16].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                }
                else if (string.Equals(status, "Error", StringComparison.OrdinalIgnoreCase))
                {
                    worksheet.Cells[row, 16].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[row, 16].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightPink);
                }

                // Format match scores with conditional formatting and percentage display
                FormatMatchScoreCell(worksheet.Cells[row, 9]);  // SQL match score
                FormatMatchScoreCell(worksheet.Cells[row, 10]); // Explanation match score
                FormatMatchScoreCell(worksheet.Cells[row, 12]); // Data match score
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

            // Color coding based on score ranges defined in business document
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
            // Using the 50 test questions from the BatchProcessingService in the provided code
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