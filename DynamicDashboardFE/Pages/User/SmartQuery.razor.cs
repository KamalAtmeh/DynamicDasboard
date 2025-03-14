using DynamicDashboardCommon.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Blazored.Toast.Services;


namespace DynamicDashboardFE.Pages.User
{
    /// <summary>
    /// Enhanced Natural Language Query component that allows users to ask questions in natural language
    /// and receive query results through a multi-step workflow.
    /// </summary>
    public partial class SmartQuery : ComponentBase
    {
        [Inject] private IConfiguration Configuration { get; set; }


        /// <summary>
        /// Defines the current step in the query workflow.
        /// </summary>
        private enum QueryStep
        {
            Input,
            Analysis,
            SqlExplanation,
            Confirmation,
            SqlGeneration,
            Execution,
            Results
        }

        // Current workflow state
        private QueryStep currentStep = QueryStep.Input;
        private string userQuestion = "";
        private bool isLoading = false;
        private bool isExamplesLoading = false;
        private string loadingMessage = "Processing your request...";
        private List<string> suggestedQuestions = new List<string>();
        private string errorMessage;

        // UI control flags
        private bool showSqlModal = false;
        private bool showUnrelatedDialog = false;
        private string unrelatedMessage = "";
        private List<string> suggestedSchemaQuestions = new List<string>();

        // Schema relevance UI elements
        private bool showPartiallyUnrelatedBanner = false;
        private string partiallyUnrelatedMessage = "";
        private List<string> partialRelatedQuestions = new List<string>();
        private List<string> schemaTopics = new List<string>();

        // Analysis step
        private AnalysisResponse analysisResponse;
        private Dictionary<string, string> resolvedAmbiguities = new Dictionary<string, string>();
        private Dictionary<string, string> adjustedParameters = new Dictionary<string, string>();

        // SQL generation step
        private SqlGenerationResponse sqlResponse;
        private SqlGenerationWithExplanationResponse sqlExplanationResponse;

        // Execution step
        private QueryExecutionResponse executionResponse;

        // Pagination
        private int currentPage = 1;
        private int pageSize;
        private int totalPages =>
            (executionResponse?.Results?.Count > 0)
                ? (int)Math.Ceiling((double)executionResponse.Results.Count / pageSize)
                : 1;
        private int startPage => Math.Max(1, currentPage - 2);
        private int endPage => Math.Min(totalPages, startPage + 4);

        // Database connection
        private int databaseId = 0;
        private Database selectedDatabase;

        // Available databases
        private List<Database> availableDatabases = new List<Database>();

        /// <summary>
        /// Initializes the component, loads configuration settings, available databases, and example questions.
        /// </summary>
        protected override async Task OnInitializedAsync()
        {
            try
            {
                // Load page size from configuration
                pageSize = Configuration.GetValue("Pagination:PageSize", 10);

                // Load available databases
                await LoadAvailableDatabases();

                // Load example questions if a database is already selected
                if (databaseId > 0)
                {
                    await LoadExampleQuestions();
                }
            }
            catch (Exception ex)
            {
                toastService.ShowError("Error initializing the application. Please try again.");
                await LogToConsole($"Error : {ex.Message}");
            }
        }

        private async Task LoadAvailableDatabases()
        {
            availableDatabases = await Http.GetFromJsonAsync<List<Database>>("api/databases");
        }

        /// <summary>
        /// Handles database selection change.
        /// </summary>
        private async Task OnDatabaseSelectionChanged(ChangeEventArgs e)
        {
            try
            {
                // Reset state on database change
                errorMessage = null;
                analysisResponse = null;
                sqlResponse = null;
                executionResponse = null;
                resolvedAmbiguities.Clear();
                adjustedParameters.Clear();
                currentStep = QueryStep.Input;

                // Parse the selected database ID
                var DBselectedId = Convert.ToInt32(e.Value);
                if (DBselectedId <= 0)
                {
                    selectedDatabase = null;
                    databaseId = 0;
                    suggestedQuestions.Clear();
                    return;
                }

                // Find the selected database in the available databases
                selectedDatabase = availableDatabases.FirstOrDefault(d => d.DatabaseID == DBselectedId);
                if (selectedDatabase == null)
                {
                    toastService.ShowWarning("Selected database not found. Please select a different database.");
                    return;
                }

                // Update database ID
                databaseId = selectedDatabase.DatabaseID;

                // Load example questions
                await LoadExampleQuestions();
                toastService.ShowSuccess($"Connected to database: {selectedDatabase.FriendlyName ?? selectedDatabase.Name}");
            }
            catch (Exception ex)
            {
                toastService.ShowError("Error selecting database. Please try again.");
                await LogToConsole($"Error :  {ex.Message}");
            }
        }

        /// <summary>
        /// Generates SQL with explanation
        /// </summary>
        /// <summary>
        /// Generates SQL with explanation
        /// </summary>
        // File: DynamicDashboardFE/Pages/User/SmartQuery.razor.cs
        // Update the GenerateSqlWithExplanation method to properly handle SQL retrieval

        private async Task GenerateSqlWithExplanation()
        {
            if (string.IsNullOrWhiteSpace(userQuestion))
                return;

            if (databaseId <= 0)
            {
                toastService.ShowWarning("Please select a database first.");
                return;
            }

            // Reset state
            errorMessage = null;
            sqlExplanationResponse = null;
            sqlResponse = null;
            executionResponse = null;
            resolvedAmbiguities.Clear();
            adjustedParameters.Clear();
            showSqlModal = false; // Reset modal state

            try
            {
                isLoading = true;
                loadingMessage = "Analyzing your question...";

                var request = new NlQueryRequest
                {
                    Question = userQuestion,
                    DatabaseId = databaseId
                };

                var response = await Http.PostAsJsonAsync("api/Query/generate-explain", request);

                if (response.IsSuccessStatusCode)
                {
                    sqlExplanationResponse = await response.Content.ReadFromJsonAsync<SqlGenerationWithExplanationResponse>();

                    if (sqlExplanationResponse != null)
                    {
                        // Log successful SQL generation
                        await LogToConsole($"SQL Generated: {sqlExplanationResponse.GeneratedSql?.Substring(0, Math.Min(100, sqlExplanationResponse.GeneratedSql?.Length ?? 0))}...");

                        // Handle unrelated questions determined by the LLM
                        if (!sqlExplanationResponse.IsSchemaRelated)
                        {
                            isLoading = false;
                            ShowUnrelatedContent(
                                sqlExplanationResponse.SchemaRelevanceMessage,
                                sqlExplanationResponse.SuggestedTopics,
                                sqlExplanationResponse.SuggestedQuestions);
                            currentStep = QueryStep.Input;
                            return;
                        }

                        // Handle partially unrelated content
                        if (sqlExplanationResponse.HasPartiallyUnrelatedContent &&
                            sqlExplanationResponse.UnrelatedQuestionParts != null &&
                            sqlExplanationResponse.UnrelatedQuestionParts.Count > 0)
                        {
                            var unrelatedParts = string.Join(", ", sqlExplanationResponse.UnrelatedQuestionParts);
                            ShowPartiallyUnrelatedContent(
                                $"Parts of your question ({unrelatedParts}) are not related to the database schema.",
                                sqlExplanationResponse.SuggestedQuestions);
                        }

                        // Initialize resolved ambiguities with default values
                        if (sqlExplanationResponse.HasAmbiguities && sqlExplanationResponse.DetectedAmbiguities != null)
                        {
                            foreach (var ambiguity in sqlExplanationResponse.DetectedAmbiguities)
                            {
                                if (ambiguity.Value.Count > 0)
                                {
                                    resolvedAmbiguities[ambiguity.Key] = ambiguity.Value[0]; // Default to first option
                                }
                            }
                        }

                        // Initialize adjusted parameters with default values
                        if (sqlExplanationResponse.AdjustableParameters != null)
                        {
                            foreach (var param in sqlExplanationResponse.AdjustableParameters)
                            {
                                adjustedParameters[param.Key] = param.Value.DefaultValue;
                            }
                        }

                        currentStep = QueryStep.SqlExplanation;

                        if (!sqlExplanationResponse.IsValid)
                        {
                            toastService.ShowWarning($"Generated SQL may not be valid: {sqlExplanationResponse.ValidationErrorMessage}");
                        }
                    }
                    else
                    {
                        toastService.ShowError("Couldn't process SQL response. Please try a different question.");
                        currentStep = QueryStep.Input;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await LogToConsole($"API Error: {errorContent}");

                    isLoading = false;
                    ShowUnrelatedContent(
                        "I couldn't understand your question in relation to the database schema. Please try rephrasing or ask something related to the available data.",
                        new List<string> { "Customer data", "Orders", "Products", "Inventory" },
                        suggestedQuestions.Count > 0 ? suggestedQuestions.Take(3).ToList() : null);
                    currentStep = QueryStep.Input;
                }
            }
            catch (Exception ex)
            {
                await LogToConsole($"Error in GenerateSqlWithExplanation: {ex.Message}");
                isLoading = false;
                ShowUnrelatedContent(
                    "An error occurred while processing your question. Please try again with a different question related to the database.",
                    null,
                    null);
                currentStep = QueryStep.Input;
            }
            finally
            {
                isLoading = false;
            }
        }

        // Update the CopySqlToClipboard method
        private async Task CopySqlToClipboard()
        {
            try
            {
                string sql = sqlExplanationResponse?.GeneratedSql;

                if (string.IsNullOrEmpty(sql))
                {
                    toastService.ShowWarning("No SQL available to copy.");
                    return;
                }

                await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", sql);
                toastService.ShowSuccess("SQL copied to clipboard!");
            }
            catch (Exception ex)
            {
                await LogToConsole($"Error copying to clipboard: {ex.Message}");
                toastService.ShowError("Failed to copy SQL to clipboard.");
            }
        }

        // Update the ShowSqlDetails method to properly display the SQL modal
        private void ShowSqlDetails()
        {
            if (sqlExplanationResponse != null && !string.IsNullOrWhiteSpace(sqlExplanationResponse.GeneratedSql))
            {
                showSqlModal = true;
            }
            else if (executionResponse != null && !string.IsNullOrWhiteSpace(executionResponse.Sql))
            {
                // Fallback to execution SQL if available
                var tempResponse = new SqlGenerationWithExplanationResponse
                {
                    GeneratedSql = executionResponse.Sql,
                    BusinessExplanation = executionResponse.ResultExplanation ?? "SQL query execution details"
                };

                sqlExplanationResponse = tempResponse;
                showSqlModal = true;
            }
            else
            {
                toastService.ShowWarning("No SQL query available to display.");
            }
        }

        // Add this method to your SmartQuery.razor.cs file
        private async Task LogToConsole(string message)
        {
            await JSRuntime.InvokeVoidAsync("console.log", message);
        }

        /// <summary>
        /// Shows dialog for completely unrelated content with suggested schema-based topics and questions
        /// </summary>
        private async void ShowUnrelatedContent(string message, List<string> suggestedTopics = null, List<string> schemaQuestions = null)
        {
            try
            {
                unrelatedMessage = message;

                // Use provided topics or default ones
                schemaTopics = suggestedTopics ?? new List<string>
    {
        "Customer data",
        "Order information",
        "Product details",
        "Inventory management",
        "Sales analytics"
    };

                // Use provided questions or fetch from available suggestions
                suggestedSchemaQuestions = schemaQuestions ??
                    (suggestedQuestions.Count > 0
                        ? suggestedQuestions.Take(3).ToList()
                        : new List<string>
                        {
                "Show me all customers",
                "What are the top 5 products by sales?",
                "How many orders were placed last month?"
                        });

                showUnrelatedDialog = true;
            }
            catch (Exception ex)
            {
               await LogToConsole("Error in ShowUnrelatedContent: " + ex.Message);
            }
        }

        /// <summary>
        /// Shows message for partially unrelated content
        /// </summary>
        private void ShowPartiallyUnrelatedContent(string message, List<string> schemaQuestions = null)
        {
            partiallyUnrelatedMessage = message;

            // Use provided questions or fetch from available suggestions
            partialRelatedQuestions = schemaQuestions ??
                (suggestedQuestions.Count > 0
                    ? suggestedQuestions.Take(3).ToList()
                    : new List<string>());

            showPartiallyUnrelatedBanner = true;
        }



        /// <summary>
        /// Generates questions based on the database schema
        /// </summary>
        private List<string> GetSchemaBasedQuestions()
        {
            // This could be enhanced to use API, but for now we'll use static suggestions or available examples
            return suggestedQuestions.Count > 0
                ? suggestedQuestions.Take(3).ToList()
                : new List<string>
                {
                    "Show me all customers",
                    "What are the top 5 products by sales?",
                    "How many orders were placed last month?"
                };
        }

        /// <summary>
        /// Analyzes user question
        /// </summary>
        private async Task AnalyzeQuestion()
        {
            // Skip analysis step and go directly to SQL generation with explanation
            await GenerateSqlWithExplanation();
        }

        /// <summary>
        /// Confirms SQL explanation and executes the query
        /// </summary>
        private async Task ConfirmSqlExplanation()
        {
            if (sqlExplanationResponse == null || string.IsNullOrWhiteSpace(sqlExplanationResponse.GeneratedSql))
                return;

            try
            {
                isLoading = true;
                loadingMessage = "Executing query...";
                errorMessage = null;

                var request = new SqlExecutionRequest
                {
                    OriginalQuestion = userQuestion,
                    DatabaseId = databaseId,
                    Sql = sqlExplanationResponse.GeneratedSql
                };

                var response = await Http.PostAsJsonAsync("api/Query/execute", request);

                if (response.IsSuccessStatusCode)
                {
                    executionResponse = await response.Content.ReadFromJsonAsync<QueryExecutionResponse>();
                    currentStep = QueryStep.Results;
                    currentPage = 1; // Reset pagination

                    if (executionResponse.Results != null && executionResponse.Results.Count > 0)
                    {
                        toastService.ShowSuccess("Query executed successfully.");
                    }
                    else
                    {
                        toastService.ShowWarning("Query executed successfully, but no results were found.");
                    }
                }
                else
                {
                    toastService.ShowError("Error executing query. Please try a different question.");
                    currentStep = QueryStep.Input;
                }
            }
            catch (Exception ex)
            {
                await LogToConsole("Error: " + ex.Message);
               toastService.ShowError("An unexpected error occurred. Please try again.");
                currentStep = QueryStep.Input;
            }
            finally
            {
                isLoading = false;
            }
        }

        /// <summary>
        /// Dismisses any displayed error messages.
        /// </summary>
        private void HandleErrorDismiss()
        {
            errorMessage = null;
        }

        /// <summary>
        /// Loads example questions from the API for the selected database.
        /// </summary>
        private async Task LoadExampleQuestions()
        {
            if (databaseId <= 0)
            {
                suggestedQuestions = new List<string>();
                return;
            }

            try
            {
                isExamplesLoading = true;
                suggestedQuestions = new List<string>();

                var response = await Http.GetFromJsonAsync<List<string>>($"api/databases/{databaseId}/example-questions/");
                if (response != null && response.Count > 0)
                {
                    suggestedQuestions = response;
                }
                else
                {
                    // Fallback to default questions
                    suggestedQuestions = new List<string>
                    {
                        "Show me the top 10 customers by total order value",
                        "What is the average order value by product category?",
                        "How many orders were placed last month?",
                        "List all products with less than 10 items in stock",
                        "Which employees had the highest sales in the last quarter?"
                    };
                }
            }
            catch (Exception ex)
            {
                await LogToConsole("Error loading example questions: " + ex.Message);
                // Use default questions if API call fails
                suggestedQuestions = new List<string>
                {
                    "Show me the top 10 customers by total order value",
                    "What is the average order value by product category?",
                    "How many orders were placed last month?",
                    "List all products with less than 10 items in stock",
                    "Which employees had the highest sales in the last quarter?"
                };
            }
            finally
            {
                isExamplesLoading = false;
            }
        }

        /// <summary>
        /// Formats column names for display by converting from snake_case or camelCase to Title Case.
        /// </summary>
        private string FormatColumnName(string column)
        {
            if (string.IsNullOrEmpty(column)) return column;

            // Replace underscores with spaces
            var result = column.Replace("_", " ");

            // Insert spaces before capital letters
            result = System.Text.RegularExpressions.Regex.Replace(result, "([a-z])([A-Z])", "$1 $2");

            // Capitalize the first letter of each word
            var textInfo = System.Globalization.CultureInfo.CurrentCulture.TextInfo;
            return textInfo.ToTitleCase(result.ToLower());
        }

        /// <summary>
        /// Formats field values for display based on their data type.
        /// </summary>
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

        /// <summary>
        /// Gets the column names from the query results.
        /// </summary>
        private List<string> GetResultColumns(List<Dictionary<string, object>> results)
        {
            if (results == null || results.Count == 0)
            {
                return new List<string>();
            }

            return results[0].Keys.ToList();
        }

        /// <summary>
        /// Gets a page of results based on the current page and page size.
        /// </summary>
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

        /// <summary>
        /// Navigates to the previous page of results.
        /// </summary>
        private void PreviousPage()
        {
            if (currentPage > 1)
            {
                currentPage--;
            }
        }

        /// <summary>
        /// Navigates to the next page of results.
        /// </summary>
        private void NextPage()
        {
            if (currentPage < totalPages)
            {
                currentPage++;
            }
        }

        /// <summary>
        /// Navigates to a specific page of results.
        /// </summary>
        private void GoToPage(int page)
        {
            if (page >= 1 && page <= totalPages)
            {
                currentPage = page;
            }
        }

        /// <summary>
        /// Exports the query results to an Excel file.
        /// </summary>
        private async Task ExportToExcel()
        {
            if (executionResponse?.Results == null || executionResponse.Results.Count == 0)
                return;

            try
            {
                var request = new ExportRequest
                {
                    Data = executionResponse.Results,
                    FileName = "QueryResults.xlsx"
                };

                var response = await Http.PostAsJsonAsync("api/export/excel", request);

                if (response.IsSuccessStatusCode)
                {
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();
                    var base64 = Convert.ToBase64String(fileBytes);
                    await JSRuntime.InvokeVoidAsync("saveAsFile", "QueryResults.xlsx", base64);
                    toastService.ShowSuccess("Data exported to Excel successfully.");
                }
                else
                {
                    toastService.ShowError("Error exporting to Excel. Please try again.");
                }
            }
            catch (Exception ex)
            {
                await LogToConsole("Error exporting to Excel: " + ex.Message);
                toastService.ShowError("Error exporting to Excel. Please try again.");
            }
        }

        /// <summary>
        /// Handles key press events, particularly the Enter key to submit questions.
        /// </summary>
        private async Task OnKeyPress(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(userQuestion) && !isLoading)
            {
                await AnalyzeQuestion();
            }
        }

        /// <summary>
        /// Uses an example question as the user's question and initiates analysis.
        /// </summary>
        private async Task UseExample(string example)
        {
            userQuestion = example;
            showUnrelatedDialog = false; // Close the dialog if open
            await AnalyzeQuestion();
        }


        /// <summary>
        /// Resets the workflow to start a new query.
        /// </summary>
        private void ResetWorkflow()
        {
            currentStep = QueryStep.Input;
            userQuestion = "";
            errorMessage = null;
            analysisResponse = null;
            sqlResponse = null;
            executionResponse = null;
            sqlExplanationResponse = null;
            resolvedAmbiguities.Clear();
            adjustedParameters.Clear();
            showSqlModal = false;
            showUnrelatedDialog = false;
        }

        /// <summary>
        /// Internal class for exporting data to Excel.
        /// </summary>
        private class ExportRequest
        {
            public List<Dictionary<string, object>> Data { get; set; }
            public string FileName { get; set; }
        }

        /// <summary>
        /// Request model for executing SQL queries.
        /// </summary>
        private class SqlExecutionRequest
        {
            public string OriginalQuestion { get; set; }
            public int DatabaseId { get; set; }
            public string Sql { get; set; }
        }
    }
}