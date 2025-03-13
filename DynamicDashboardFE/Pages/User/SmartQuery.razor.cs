using DynamicDashboardCommon.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Blazored.Toast.Services;
using DynamicDashboardFE.Utilities;

namespace DynamicDashboardFE.Pages.User
{
    /// <summary>
    /// Enhanced Natural Language Query component that allows users to ask questions in natural language
    /// and receive query results through a multi-step workflow.
    /// </summary>
    public partial class SmartQuery : ComponentBase
    {
        [Inject] private IConfiguration Configuration { get; set; }
        [Inject] private Notifications Notifications { get; set; }

        /// <summary>
        /// Defines the current step in the query workflow.
        /// </summary>

        // Update enum
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
        private string dbServer = "";
        private string dbName = "";
        private string FriendlyName = "";
        private string dbAuthType = "windows";
        private string dbUsername = "";
        private string dbPassword = "";
        private string connectionStatus = "";
        private bool connectionSuccessful = false;
        private int databaseId = 0;

        // Available databases
        private List<Database> availableDatabases = new List<Database>();
        private Database selectedDatabase;

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
            catch (Exception)
            {
                Notifications.ShowError("Error initializing the application. Please try again.");
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
                    connectionSuccessful = false;
                    dbServer = "";
                    dbName = "";
                    FriendlyName = "";
                    connectionStatus = "";
                    suggestedQuestions.Clear();
                    return;
                }

                // Find the selected database in the available databases
                selectedDatabase = availableDatabases.FirstOrDefault(d => d.DatabaseID == DBselectedId);
                if (selectedDatabase == null)
                {
                    Notifications.ShowWarning("Selected database not found. Please select a different database.");
                    return;
                }

                // Update UI fields with database information
                databaseId = selectedDatabase.DatabaseID;
                dbServer = selectedDatabase.ServerAddress;
                dbName = selectedDatabase.Name;
                FriendlyName = selectedDatabase.FriendlyName ?? selectedDatabase.Name;

                // Test the connection automatically
                await TestDatabaseConnection();

                // If connection is successful, load example questions
                if (connectionSuccessful)
                {
                    await LoadExampleQuestions();
                    Notifications.ShowSuccess($"Connected to database: {FriendlyName}");
                }
            }
            catch (Exception)
            {
                Notifications.ShowError("Error selecting database. Please try again.");
            }
        }

        // Add this method to generate SQL with explanation
        private async Task GenerateSqlWithExplanation()
        {
            if (string.IsNullOrWhiteSpace(userQuestion))
                return;

            if (databaseId <= 0)
            {
                Notifications.ShowWarning("Please select a database first.");
                return;
            }

            // Check if connection is established
            if (!connectionSuccessful)
            {
                await TestDatabaseConnection();
                if (!connectionSuccessful)
                {
                    Notifications.ShowError("Database connection failed. Please check your settings.");
                    return;
                }
            }

            // Reset state
            errorMessage = null;
            sqlExplanationResponse = null;
            sqlResponse = null;
            executionResponse = null;
            resolvedAmbiguities.Clear();
            adjustedParameters.Clear();

            try
            {
                isLoading = true;
                loadingMessage = "Thinking ^_^ ...";

                var request = new NlQueryRequest
                {
                    Question = userQuestion,
                    DatabaseId = databaseId
                };

                var response = await Http.PostAsJsonAsync("api/Query/generate-explain", request);

                if (response.IsSuccessStatusCode)
                {
                    sqlExplanationResponse = await response.Content.ReadFromJsonAsync<SqlGenerationWithExplanationResponse>();

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
                        Notifications.ShowWarning($"Generated SQL may not be valid: {sqlExplanationResponse.ValidationErrorMessage}");
                    }
                }
                else
                {
                    Notifications.ShowError("Error. Please try again, with different question");
                    //todo i need to suggest questions to the user.
                    currentStep = QueryStep.Input;
                }
            }
            catch (Exception ex)
            {
                Notifications.ShowError("Errpr, Please try again. you can use suggested questions");
                //todo i need to suggest questions to the user.
                currentStep = QueryStep.Input;
            }
            finally
            {
                isLoading = false;
            }
        }

        // Update AnalyzeQuestion method to use the new flow
        private async Task AnalyzeQuestion()
        {
            // Skip analysis step and go directly to SQL generation with explanation
            await GenerateSqlWithExplanation();
        }

        // Add confirmation method
        private async Task ConfirmSqlExplanation()
        {
            if (sqlExplanationResponse == null || string.IsNullOrWhiteSpace(sqlExplanationResponse.GeneratedSql))
                return;

            try
            {
                isLoading = true;
                loadingMessage = "Loading your data ^_^ ...";
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
                        Notifications.ShowSuccess("Query executed successfully.");
                    }
                    else
                    {
                        Notifications.ShowWarning("Query executed successfully, but no results were found.");
                    }
                }
                else
                {
                    Notifications.ShowError("Error executing query. Please check your database connection.");
                    currentStep = QueryStep.SqlExplanation;
                }
            }
            catch (Exception)
            {
                Notifications.ShowError("An unexpected error occurred. Please try again.");
                currentStep = QueryStep.SqlExplanation;
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
            }
            catch (Exception ex)
            {
                Notifications.ShowWarning("Could not load example questions. Default examples will be used. ");
                Console.WriteLine(ex.Message + ex.StackTrace);
            }
            finally
            {
                isExamplesLoading = false;
            }
        }

        /// <summary>
        /// Tests the database connection using the provided credentials.
        /// </summary>
        private async Task TestDatabaseConnection()
        {
            try
            {
                isLoading = true;
                loadingMessage = "Testing database connection...";
                connectionStatus = "";

                // If using an existing database from the dropdown
                Database database;
                if (selectedDatabase != null && databaseId > 0)
                {
                    database = new Database
                    {
                        DatabaseID = databaseId,
                        ServerAddress = selectedDatabase.ServerAddress,
                        Name = selectedDatabase.Name,
                        FriendlyName = selectedDatabase.FriendlyName,
                        TypeID = selectedDatabase.TypeID,
                        Username = dbAuthType == "sql" ? dbUsername : selectedDatabase.Username,
                        EncryptedCredentials = dbAuthType == "sql" ? dbPassword : selectedDatabase.EncryptedCredentials
                    };
                }
                else
                {
                    // For manual connection
                    database = new Database
                    {
                        ServerAddress = dbServer,
                        Name = dbName,
                        FriendlyName = FriendlyName,
                        TypeID = 1,   // Default to SQL Server
                        Username = dbAuthType == "sql" ? dbUsername : "",
                        EncryptedCredentials = dbAuthType == "sql" ? dbPassword : "",
                        DatabaseID = 0 // 0 indicates a new connection test
                    };
                }

                var response = await Http.PostAsJsonAsync("api/databases/test-connection", database);

                if (response.IsSuccessStatusCode)
                {
                    connectionStatus = "Connection successful";
                    connectionSuccessful = true;
                    Notifications.ShowSuccess("Database connection successful!");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    connectionStatus = "Connection failed";
                    connectionSuccessful = false;
                    Notifications.ShowError("Database connection failed. Please check your settings.");
                }
            }
            catch (Exception)
            {
                connectionStatus = "Connection failed";
                connectionSuccessful = false;
                Notifications.ShowError("Error testing database connection. Please try again.");
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
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
                    Notifications.ShowSuccess("Data exported to Excel successfully.");
                }
                else
                {
                    Notifications.ShowError("Error exporting to Excel. Please try again.");
                }
            }
            catch (Exception)
            {
                Notifications.ShowError("Error exporting to Excel. Please try again.");
            }
        }

        /// <summary>
        /// Handles key press events, particularly the Enter key to submit questions.
        /// </summary>
        private async Task OnKeyPress(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && currentStep == QueryStep.Input)
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
            await AnalyzeQuestion();
        }

        /// <summary>
        /// Copies the generated SQL to the clipboard.
        /// </summary>
        private async Task CopySqlToClipboard()
        {
            var sql = sqlResponse?.GeneratedSql;
            if (string.IsNullOrEmpty(sql)) return;

            try
            {
                await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", sql);
                Notifications.ShowSuccess("SQL copied to clipboard!");
            }
            catch (Exception)
            {
                Notifications.ShowError("Failed to copy SQL to clipboard.");
            }
        }

        /// <summary>
        /// Step 1: Analyzes the natural language question and generates an explanation.
        /// </summary>
        //private async Task AnalyzeQuestion_OLD()
        //{
        //    if (string.IsNullOrWhiteSpace(userQuestion))
        //        return;

        //    if (databaseId <= 0)
        //    {
        //        Notifications.ShowWarning("Please select a database first.");
        //        return;
        //    }

        //    // Check if connection is established
        //    if (!connectionSuccessful)
        //    {
        //        await TestDatabaseConnection();
        //        if (!connectionSuccessful)
        //        {
        //            Notifications.ShowError("Database connection failed. Please check your settings.");
        //            return;
        //        }
        //    }

        //    // Reset state
        //    errorMessage = null;
        //    analysisResponse = null;
        //    sqlResponse = null;
        //    executionResponse = null;
        //    resolvedAmbiguities.Clear();
        //    adjustedParameters.Clear();

        //    try
        //    {
        //        currentStep = QueryStep.Analysis;
        //        isLoading = true;
        //        loadingMessage = "Analyzing your question...";

        //        var request = new NlQueryRequest
        //        {
        //            Question = userQuestion,
        //            DatabaseId = databaseId
        //        };

        //        var response = await Http.PostAsJsonAsync("api/Query/analyze", request);

        //        if (response.IsSuccessStatusCode)
        //        {
        //            analysisResponse = await response.Content.ReadFromJsonAsync<AnalysisResponse>();

        //            // Initialize resolved ambiguities with default values
        //            if (analysisResponse.HasAmbiguities && analysisResponse.DetectedAmbiguities != null)
        //            {
        //                foreach (var ambiguity in analysisResponse.DetectedAmbiguities)
        //                {
        //                    if (ambiguity.Value.Count > 0)
        //                    {
        //                        resolvedAmbiguities[ambiguity.Key] = ambiguity.Value[0]; // Default to first option
        //                    }
        //                }
        //            }

        //            // Initialize adjusted parameters with default values
        //            if (analysisResponse.AdjustableParameters != null)
        //            {
        //                foreach (var param in analysisResponse.AdjustableParameters)
        //                {
        //                    adjustedParameters[param.Key] = param.Value.DefaultValue;
        //                }
        //            }

        //            currentStep = QueryStep.Confirmation;
        //        }
        //        else
        //        {
        //            Notifications.ShowError("Error analyzing your question. Please try again.");
        //            currentStep = QueryStep.Input;
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        Notifications.ShowError("An unexpected error occurred. Please try again.");
        //        currentStep = QueryStep.Input;
        //    }
        //    finally
        //    {
        //        isLoading = false;
        //    }
        //}

        /// <summary>
        /// Step 2: Confirms understanding and generates SQL based on user selections.
        /// </summary>
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

                var response = await Http.PostAsJsonAsync("api/Query/generate", request);

                if (response.IsSuccessStatusCode)
                {
                    sqlResponse = await response.Content.ReadFromJsonAsync<SqlGenerationResponse>();
                    Notifications.ShowSuccess("SQL generated successfully.");
                }
                else
                {
                    Notifications.ShowError("Error generating SQL query. Please try again.");
                    currentStep = QueryStep.Confirmation;
                }
            }
            catch (Exception)
            {
                Notifications.ShowError("An unexpected error occurred. Please try again.");
                currentStep = QueryStep.Confirmation;
            }
            finally
            {
                isLoading = false;
            }
        }

        /// <summary>
        /// Step 3: Executes the generated SQL query and displays results.
        /// </summary>
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

                var response = await Http.PostAsJsonAsync("api/Query/execute", request);

                if (response.IsSuccessStatusCode)
                {
                    executionResponse = await response.Content.ReadFromJsonAsync<QueryExecutionResponse>();
                    currentStep = QueryStep.Results;
                    currentPage = 1; // Reset pagination

                    if (executionResponse.Results != null && executionResponse.Results.Count > 0)
                    {
                        Notifications.ShowSuccess("Query executed successfully.");
                    }
                    else
                    {
                        Notifications.ShowWarning("Query executed successfully, but no results were found.");
                    }
                }
                else
                {
                    Notifications.ShowError("Error executing query. Please check your database connection.");
                    currentStep = QueryStep.SqlGeneration;
                }
            }
            catch (Exception)
            {
                Notifications.ShowError("An unexpected error occurred. Please try again.");
                currentStep = QueryStep.SqlGeneration;
            }
            finally
            {
                isLoading = false;
            }
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
            resolvedAmbiguities.Clear();
            adjustedParameters.Clear();
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
        /// Request model for confirming understanding and generating SQL.
        /// </summary>
        private class NlQueryConfirmationRequest
        {
            public string OriginalQuestion { get; set; }
            public int DatabaseId { get; set; }
            public string ConfirmedUnderstanding { get; set; }
            public Dictionary<string, string> ResolvedAmbiguities { get; set; }
            public Dictionary<string, string> AdjustedParameters { get; set; }
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