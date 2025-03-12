using DynamicDashboardCommon.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Text.Json;
using DynamicDashboardFE.Utilities;


namespace DynamicDashboardFE.Pages.User
{
    public partial class EnhancedSmartNlQuery : ComponentBase
    {

        private enum QueryStep
        {
            Input,
            Analysis,
            Confirmation,
            SqlGeneration,
            Execution,
            Results
        }

        // Mode selection
        private bool useEnhancedMode = false;

        // State management for both modes
        private string activeTab = "nl";
        private string userQuestion = "";
        private string directSqlQuery = "";
        private bool isLoading = false;
        private bool isExamplesLoading = false;
        private string loadingMessage = "Processing your request...";
        private List<string> examples = new List<string>();
        private string errorMessage;

        // Original mode - query response
        private NlQueryResponse queryResponse;

        // Enhanced mode - Analysis step
        private QueryStep currentStep = QueryStep.Input;
        private AnalysisResponse analysisResponse;
        private Dictionary<string, string> resolvedAmbiguities = new Dictionary<string, string>();
        private Dictionary<string, string> adjustedParameters = new Dictionary<string, string>();

        // Enhanced mode - SQL generation step
        private SqlGenerationResponse sqlResponse;

        // Enhanced mode - Execution step
        private QueryExecutionResponse executionResponse;

        // Pagination for both modes
        private int currentPage = 1;
        private int pageSize = 10;
        private int totalPages =>
            useEnhancedMode
                ? (executionResponse?.Results?.Count > 0 ? (int)Math.Ceiling((double)executionResponse.Results.Count / pageSize) : 1)
                : (queryResponse?.Results?.Count > 0 ? (int)Math.Ceiling((double)queryResponse.Results.Count / pageSize) : 1);
        private int startPage => Math.Max(1, currentPage - 2);
        private int endPage => Math.Min(totalPages, startPage + 4);

        // Database connection for both modes
        //temp
        private string dbServer = "(LocalDB)\\MSSQLLocalDB";
        private string dbName = "ECommerceDB";
        private string FriendlyName = "ECommerce DataBase";
        private string dbAuthType = "windows";
        private string dbUsername = "";
        private string dbPassword = "";
        private string connectionStatus = "";
        private bool connectionSuccessful = false;
        private int databaseId = 5; // Temporary hardcoded database ID

        // Batch processing for original mode
        private IBrowserFile selectedFile;
        private bool batchProcessingComplete = false;
        private int batchQuestionsCount = 0;
        private byte[] processedExcelBytes;

        protected override async Task OnInitializedAsync()
        {
            await LoadExampleQuestions();
        }

        private void SetActiveTab(string tab)
        {
            activeTab = tab;
            // Reset pagination when changing tabs
            currentPage = 1;
        }

        private void HandleErrorDismiss()
        {
            if (useEnhancedMode)
            {
                ResetWorkflow();
            }
            else
            {
                errorMessage = null;
            }
        }

        private async Task LoadExampleQuestions()
        {
            try
            {
                isExamplesLoading = true;

                // For enhanced mode, try to get examples from the enhanced endpoint
                if (useEnhancedMode)
                {
                    try
                    {
                        var response = await Http.GetFromJsonAsync<List<string>>($"api/enhanced/examples/{databaseId}");
                        if (response != null && response.Count > 0)
                        {
                            examples = response;
                            isExamplesLoading = false;
                            return;
                        }
                    }
                    catch
                    {
                        // Fall back to default examples
                    }
                }

                // Default examples for both modes
                examples = new List<string>
           {
               "Show me the top 10 customers by total order value",
               "What is the average order value by product category?",
               "How many orders were placed last month?",
               "List all products with less than 10 items in stock",
               "Which customers made at least 3 purchases in the last 6 months?"
           };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading examples: {ex.Message}");
            }
            finally
            {
                isExamplesLoading = false;
            }
        }

        private RenderFragment RenderResultBasedOnViewingType()
        {
            if (queryResponse == null)
                return null;

            return queryResponse.RecommendedDataViewingTypeID switch
            {
                (int)DataViewingTypeEnum.Number => builder =>
                {
                    builder.OpenElement(0, "div");
                    builder.AddAttribute(1, "class", "card shadow-sm border-0 mb-4");
                    builder.OpenElement(2, "div");
                    builder.AddAttribute(3, "class", "card-header bg-light py-3");
                    builder.OpenElement(4, "h5");
                    builder.AddAttribute(5, "class", "m-0 font-weight-bold");
                    builder.AddContent(6, "Result");
                    builder.CloseElement(); // Close h5
                    builder.CloseElement(); // Close card-header

                    builder.OpenElement(7, "div");
                    builder.AddAttribute(8, "class", "card-body text-center");
                    builder.OpenElement(9, "h1");
                    builder.AddAttribute(10, "class", "display-4 text-primary");
                    builder.AddContent(11, queryResponse.FormattedResult);
                    builder.CloseElement(); // Close h1
                    builder.CloseElement(); // Close card-body

                    builder.CloseElement(); // Close card
                }
                ,
                (int)DataViewingTypeEnum.Label => builder =>
                {
                    builder.OpenElement(0, "div");
                    builder.AddAttribute(1, "class", "alert alert-info");
                    builder.AddContent(2, queryResponse.FormattedResult);
                    builder.CloseElement();
                }
                ,
                _ => null
            };
        }

        #region Common Functionality (Used by Both Modes)

        private async Task TestDatabaseConnection()
        {
            try
            {
                isLoading = true;
                loadingMessage = "Testing database connection...";

                var database = new Database
                {
                    ServerAddress = dbServer,
                    Name = dbName,
                    FriendlyName = FriendlyName,
                    TypeID = 1,   // temp Default to SQL Server
                    Username = dbUsername,
                    EncryptedCredentials = dbPassword,
                    DatabaseID = 5,
                    Description = string.Empty,
                    ConnectionString = string.Empty,
                    CreatedAt = DateTime.Now,
                    CreatedBy = 3,
                    DatabaseTypeName = "SQL",
                    DBCreationScript = " string.empy" // 0 indicates a new connection test
                };

                var response = await Http.PostAsJsonAsync("api/databases/test-connection", database);

                if (response.IsSuccessStatusCode)
                {
                    connectionStatus = "Connection successful";
                    connectionSuccessful = true;

                    // In a real implementation, get the database ID from the response
                    databaseId = 5; // Temporary hardcoded value
                }
                else
                {
                    connectionStatus = "Connection failed";
                    connectionSuccessful = false;
                }
            }
            catch (Exception ex)
            {
                connectionStatus = $"Connection failed: {ex.Message}";
                connectionSuccessful = false;
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private string FormatColumnName(string column)
        {
            // Convert snake_case or camelCase to Title Case
            if (string.IsNullOrEmpty(column)) return column;

            // Replace underscores with spaces
            var result = column.Replace("_", " ");

            // Insert spaces before capital letters
            result = System.Text.RegularExpressions.Regex.Replace(result, "([a-z])([A-Z])", "$1 $2");

            // Capitalize the first letter of each word
            var textInfo = System.Globalization.CultureInfo.CurrentCulture.TextInfo;
            return textInfo.ToTitleCase(result.ToLower());
        }

        private string FormatValue(object value)
        {
            if (value == null) return "";

            if (value is DateTime dateTime)
            {
                return dateTime.ToString("yyyy-MM-dd");
            }

            if (value is decimal decimalValue)
            {
                return decimalValue.ToString("0.00");
            }

            return value.ToString();
        }

        private List<string> GetResultColumns(List<Dictionary<string, object>> results)
        {
            if (results == null || results.Count == 0)
            {
                return new List<string>();
            }

            return results[0].Keys.ToList();
        }

        private List<Dictionary<string, object>> GetPagedResults(List<Dictionary<string, object>> results)
        {
            if (results == null || results.Count == 0)
            {
                return new List<Dictionary<string, object>>();
            }

            return results
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        private void PreviousPage()
        {
            if (currentPage > 1)
            {
                currentPage--;
            }
        }

        private void NextPage()
        {
            if (currentPage < totalPages)
            {
                currentPage++;
            }
        }

        private void GoToPage(int page)
        {
            if (page >= 1 && page <= totalPages)
            {
                currentPage = page;
            }
        }

        private async Task ExportToExcel()
        {
            var dataToExport = useEnhancedMode && executionResponse?.Results != null
                ? executionResponse.Results
                : queryResponse?.Results;

            if (dataToExport == null || dataToExport.Count == 0) return;

            try
            {
                var request = new ExportRequest
                {
                    Data = dataToExport,
                    FileName = "QueryResults.xlsx"
                };

                var response = await Http.PostAsJsonAsync("api/export/excel", request);

                if (response.IsSuccessStatusCode)
                {
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();
                    var base64 = Convert.ToBase64String(fileBytes);
                    await JSRuntime.InvokeVoidAsync("saveAsFile", "QueryResults.xlsx", base64);
                }
                else
                {
                    errorMessage = "Error exporting to Excel";
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error exporting to Excel: {ex.Message}";
            }
        }

        private async Task OnKeyPress(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {
                if (useEnhancedMode && currentStep == QueryStep.Input)
                {
                    await AnalyzeQuestion();
                }
                else
                {
                    await ProcessQuestion();
                }
            }
        }

        private void UseExample(string example)
        {
            userQuestion = example;

            if (useEnhancedMode)
            {
                AnalyzeQuestion();
            }
            else
            {
                ProcessQuestion();
            }
        }

        #endregion

        #region Original Mode Functionality

        private async Task ProcessQuestion()
        {
            if (string.IsNullOrWhiteSpace(userQuestion))
            {
                return;
            }

            // Check if connection is established
            if (!connectionSuccessful)
            {
                await TestDatabaseConnection();
                if (!connectionSuccessful) return;
            }

            isLoading = true;
            loadingMessage = "Analyzing your question and generating a query...";
            queryResponse = null;
            currentPage = 1;

            try
            {
                // Create the request using the database connection info and question
                var request = new NlQueryRequest
                {
                    Question = userQuestion,
                    DatabaseId = databaseId
                };

                var response = await Http.PostAsJsonAsync("api/EnhancedNlQuery/process", request);

                if (response.IsSuccessStatusCode)
                {
                    queryResponse = await response.Content.ReadFromJsonAsync<NlQueryResponse>();
                    Console.WriteLine($"Query Response: {JsonSerializer.Serialize(queryResponse)}");

                    if (queryResponse.Results == null || queryResponse.Results.Count == 0)
                    {
                        Console.WriteLine("No results found in the response.");
                    }
                }
                else
                {
                    queryResponse = new NlQueryResponse
                    {
                        Success = false,
                        ErrorMessage = $"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}"
                    };
                    Console.WriteLine($"API Error: {queryResponse.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                queryResponse = new NlQueryResponse
                {
                    Success = false,
                    ErrorMessage = $"An unexpected error occurred: {ex.Message}"
                };
                Console.WriteLine($"Exception: {ex.Message}");
            }
            finally
            {
                isLoading = false;
                StateHasChanged(); // Force UI update
            }
        }

        private async Task ExecuteDirectSql()
        {
            if (string.IsNullOrWhiteSpace(directSqlQuery))
            {
                return;
            }

            // Check if connection is established
            if (!connectionSuccessful)
            {
                await TestDatabaseConnection();
                if (!connectionSuccessful) return;
            }

            isLoading = true;
            loadingMessage = "Executing SQL query...";
            queryResponse = null;
            currentPage = 1;

            try
            {
                // Create a direct SQL execution request
                var request = new DirectSqlRequest
                {
                    SqlQuery = directSqlQuery,
                    DbType = "SQLServer",  // Based on the selected connection type
                    DatabaseId = databaseId
                };

                var response = await Http.PostAsJsonAsync("api/query/execute", request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<DirectSqlResult>();

                    // Create an NlQueryResponse from the direct SQL result
                    queryResponse = new NlQueryResponse
                    {
                        GeneratedSql = directSqlQuery,
                        Results = result.Data,
                        Success = true
                    };
                }
                else
                {
                    queryResponse = new NlQueryResponse
                    {
                        Success = false,
                        ErrorMessage = $"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}"
                    };
                }
            }
            catch (Exception ex)
            {
                queryResponse = new NlQueryResponse
                {
                    Success = false,
                    ErrorMessage = $"An unexpected error occurred: {ex.Message}"
                };
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private void OnFileSelected(InputFileChangeEventArgs args)
        {
            selectedFile = args.File;
            batchProcessingComplete = false;
        }

        private async Task ProcessExcelFile()
        {
            if (selectedFile == null)
            {
                return;
            }

            // Check if connection is established
            if (!connectionSuccessful)
            {
                await TestDatabaseConnection();
                if (!connectionSuccessful) return;
            }

            isLoading = true;
            loadingMessage = "Processing Excel file...";
            batchProcessingComplete = false;

            try
            {
                var content = new MultipartFormDataContent();
                content.Add(new StreamContent(selectedFile.OpenReadStream()), "file", selectedFile.Name);
                content.Add(new StringContent("SQLServer"), "dbType");

                var response = await Http.PostAsync("api/batchprocessing/process", content);

                if (response.IsSuccessStatusCode)
                {
                    processedExcelBytes = await response.Content.ReadAsByteArrayAsync();
                    batchQuestionsCount = new Random().Next(10, 30); // Simulated count
                    batchProcessingComplete = true;
                }
                else
                {
                    await JSRuntime.InvokeVoidAsync("alert", $"Error processing file: {await response.Content.ReadAsStringAsync()}");
                }
            }
            catch (Exception ex)
            {
                await JSRuntime.InvokeVoidAsync("alert", $"Error processing file: {ex.Message}");
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private async Task DownloadSampleTemplate()
        {
            try
            {
                var response = await Http.GetAsync("api/batchprocessing/template");

                if (response.IsSuccessStatusCode)
                {
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();
                    var base64 = Convert.ToBase64String(fileBytes);
                    await JSRuntime.InvokeVoidAsync("saveAsFile", "Questions_Template.xlsx", base64);
                }
                else
                {
                    await JSRuntime.InvokeVoidAsync("alert", "Error downloading template");
                }
            }
            catch (Exception ex)
            {
                await JSRuntime.InvokeVoidAsync("alert", $"Error: {ex.Message}");
            }
        }

        private async Task Download50TestQuestions()
        {
            try
            {
                var response = await Http.GetAsync("api/batchprocessing/test-questions");

                if (response.IsSuccessStatusCode)
                {
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();
                    var base64 = Convert.ToBase64String(fileBytes);
                    await JSRuntime.InvokeVoidAsync("saveAsFile", "50_Test_Questions.xlsx", base64);
                }
                else
                {
                    await JSRuntime.InvokeVoidAsync("alert", "Error downloading test questions");
                }
            }
            catch (Exception ex)
            {
                await JSRuntime.InvokeVoidAsync("alert", $"Error: {ex.Message}");
            }
        }

        private async Task DownloadProcessedExcel()
        {
            if (processedExcelBytes == null || processedExcelBytes.Length == 0)
            {
                return;
            }

            try
            {
                var base64 = Convert.ToBase64String(processedExcelBytes);
                await JSRuntime.InvokeVoidAsync("saveAsFile", "Processed_Questions.xlsx", base64);
            }
            catch (Exception ex)
            {
                await JSRuntime.InvokeVoidAsync("alert", $"Error downloading file: {ex.Message}");
            }
        }

        private async Task CopySqlToClipboard()
        {
            var sql = useEnhancedMode ? sqlResponse?.GeneratedSql : queryResponse?.GeneratedSql;
            if (string.IsNullOrEmpty(sql)) return;

            await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", sql);
            await JSRuntime.InvokeVoidAsync("alert", "SQL copied to clipboard!");
        }

        #endregion

        #region Enhanced Mode Functionality

        private async Task AnalyzeQuestion()
        {
            if (string.IsNullOrWhiteSpace(userQuestion))
                return;

            // Check if connection is established
            if (!connectionSuccessful)
            {
                await TestDatabaseConnection();
                if (!connectionSuccessful) return;
            }

            // Reset state
            errorMessage = null;
            analysisResponse = null;
            sqlResponse = null;
            executionResponse = null;
            resolvedAmbiguities.Clear();
            adjustedParameters.Clear();

            try
            {
                currentStep = QueryStep.Analysis;
                isLoading = true;
                loadingMessage = "Analyzing your question...";

                var request = new NlQueryRequest
                {
                    Question = userQuestion,
                    DatabaseId = databaseId
                };

                var response = await Http.PostAsJsonAsync("api/enhancednlquery/analyze", request);

                if (response.IsSuccessStatusCode)
                {
                    analysisResponse = await response.Content.ReadFromJsonAsync<AnalysisResponse>();

                    // Initialize resolved ambiguities with default values
                    if (analysisResponse.HasAmbiguities && analysisResponse.DetectedAmbiguities != null)
                    {
                        foreach (var ambiguity in analysisResponse.DetectedAmbiguities)
                        {
                            if (ambiguity.Value.Count > 0)
                            {
                                resolvedAmbiguities[ambiguity.Key] = ambiguity.Value[0]; // Default to first option
                            }
                        }
                    }

                    // Initialize adjusted parameters with default values
                    if (analysisResponse.AdjustableParameters != null)
                    {
                        foreach (var param in analysisResponse.AdjustableParameters)
                        {
                            adjustedParameters[param.Key] = param.Value.DefaultValue;
                        }
                    }

                    currentStep = QueryStep.Confirmation;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    errorMessage = $"Error: {response.StatusCode} - {errorContent}";
                    currentStep = QueryStep.Input;
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"An unexpected error occurred: {ex.Message}";
                currentStep = QueryStep.Input;
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task ConfirmUnderstanding()
        {
            if (analysisResponse == null)
                return;

            try
            {
                currentStep = QueryStep.SqlGeneration;
                isLoading = true;
                loadingMessage = "Generating SQL query...";
                errorMessage = null;

                var request = new NlQueryConfirmationRequest
                {
                    OriginalQuestion = userQuestion,
                    DatabaseId = databaseId,
                    ConfirmedUnderstanding = analysisResponse.Explanation,
                    ResolvedAmbiguities = resolvedAmbiguities,
                    AdjustedParameters = adjustedParameters
                };

                var response = await Http.PostAsJsonAsync("api/enhancednlquery/generate", request);

                if (response.IsSuccessStatusCode)
                {
                    sqlResponse = await response.Content.ReadFromJsonAsync<SqlGenerationResponse>();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    errorMessage = $"Error generating SQL: {response.StatusCode} - {errorContent}";
                    currentStep = QueryStep.Confirmation;
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"An unexpected error occurred: {ex.Message}";
                currentStep = QueryStep.Confirmation;
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task ExecuteQuery()
        {
            if (sqlResponse == null || string.IsNullOrWhiteSpace(sqlResponse.GeneratedSql))
                return;

            try
            {
                currentStep = QueryStep.Execution;
                isLoading = true;
                loadingMessage = "Executing query...";
                errorMessage = null;

                var request = new SqlExecutionRequest
                {
                    OriginalQuestion = userQuestion,
                    DatabaseId = databaseId,
                    Sql = sqlResponse.GeneratedSql
                };

                var response = await Http.PostAsJsonAsync("api/enhancednlquery/execute", request);

                if (response.IsSuccessStatusCode)
                {
                    executionResponse = await response.Content.ReadFromJsonAsync<QueryExecutionResponse>();
                    currentStep = QueryStep.Results;
                    currentPage = 1; // Reset pagination
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    errorMessage = $"Error executing query: {response.StatusCode} - {errorContent}";
                    currentStep = QueryStep.SqlGeneration;
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"An unexpected error occurred: {ex.Message}";
                currentStep = QueryStep.SqlGeneration;
            }
            finally
            {
                isLoading = false;
            }
        }

        private void ResetWorkflow()
        {
            currentStep = QueryStep.Input;
            userQuestion = "";
            errorMessage = null;
            analysisResponse = null;
            sqlResponse = null;
            executionResponse = null;
            resolvedAmbiguities.Clear();
            adjustedParameters.Clear();
        }

        #endregion

        #region Supporting Classes

        // Original mode classes
        private class DirectSqlRequest
        {
            public string SqlQuery { get; set; }
            public string DbType { get; set; }
            public int DatabaseId { get; set; }
        }

        private class DirectSqlResult
        {
            public List<Dictionary<string, object>> Data { get; set; }
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
        }

        // Enhanced mode classes
        private class NlQueryConfirmationRequest
        {
            public string OriginalQuestion { get; set; }
            public int DatabaseId { get; set; }
            public string ConfirmedUnderstanding { get; set; }
            public Dictionary<string, string> ResolvedAmbiguities { get; set; }
            public Dictionary<string, string> AdjustedParameters { get; set; }
        }

        private class SqlExecutionRequest
        {
            public string OriginalQuestion { get; set; }
            public int DatabaseId { get; set; }
            public string Sql { get; set; }
        }

        // Shared classes
        private class ExportRequest
        {
            public List<Dictionary<string, object>> Data { get; set; }
            public string FileName { get; set; }
        }

        #endregion
    }
}
