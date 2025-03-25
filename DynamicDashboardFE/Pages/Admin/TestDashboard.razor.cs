using Blazored.Toast.Services;
using DynamicDashboardCommon.Helper;
using DynamicDashboardCommon.Models;
using DynamicDashboardCommon.Models.TestAutomation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DynamicDashboardFE.Pages.Admin
{
    public partial class TestDashboard
    {


        private string activeTab = "upload";
        private string activeComparisonTab = "sql";
        private string activeDatasetTab = "expected";
        private string detailsFilter = "all";
        private string jsonTestCases;

        private List<Database> databases = new List<Database>();
        private List<TestAutomationJob> testJobs;
        private List<TestAutomationDetail> testDetails;
        private List<TestAutomationDetail> filteredDetails => FilterTestDetails();

        private int selectedDatabaseId;
        private string selectedLlmProvider;
        private IBrowserFile selectedFile;
        private TestAutomationJob selectedJob;
        private TestAutomationDetail selectedDetail;
        private DatasetComparisonResult comparisonData;

        private bool isLoading;
        private bool isUploading;
        private bool isLoadingDetails;
        private bool isLoadingDatasets;
        private bool showComparisonModal;

        private int currentPage = 1;
        private int pageSize = 10;
        private int totalDetailsCount;
        private int totalPages => (int)Math.Ceiling((double)totalDetailsCount / pageSize);
        private int startPage => Math.Max(1, currentPage - 2);
        private int endPage => Math.Min(totalPages, startPage + 4);


        //Comparison DataSets
        private bool showComparisonView;
        private TestAutomationDetail comparisonDetail;
        private List<Dictionary<string, object>> expectedDataset;
        private List<Dictionary<string, object>> actualDataset;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                await LoadDatabases();
                await LoadTestJobs();
            }
            catch (Exception ex)
            {
                toastService.ShowError("Error initializing test dashboard: " + ex.Message);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await JSRuntime.InvokeVoidAsync("import", "/js/testDashboard.js");
            }

            if (selectedJob != null && activeTab == "results")
            {
                await JSRuntime.InvokeVoidAsync("renderSuccessRateChart",
                    selectedJob.SuccessCount,
                    selectedJob.TotalQuestions - selectedJob.SuccessCount);
            }
        }

        private async Task LoadDatabases()
        {
            try
            {
                databases = await Http.GetFromJsonAsync<List<Database>>("api/databases");
            }
            catch (Exception ex)
            {
                toastService.ShowError("Error loading databases: " + ex.Message);
                databases = new List<Database>();
            }
        }

        private async Task LoadTestJobs()
        {
            try
            {
                isLoading = true;
                testJobs = await Http.GetFromJsonAsync<List<TestAutomationJob>>("api/testautomation/jobs");
            }
            catch (Exception ex)
            {
                toastService.ShowError("Error loading test jobs: " + ex.Message);
                testJobs = new List<TestAutomationJob>();
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private async Task RefreshJobs()
        {
            await LoadTestJobs();
            toastService.ShowSuccess("Test jobs refreshed");
        }

        private void SetActiveTab(string tab)
        {
            activeTab = tab;
        }

        private void SetActiveComparisonTab(string tab)
        {
            activeComparisonTab = tab;
        }

        private void SetActiveDatasetTab(string tab)
        {
            activeDatasetTab = tab;
        }

        private void SetDetailsFilter(string filter)
        {
            detailsFilter = filter;
        }

        private List<TestAutomationDetail> FilterTestDetails()
        {
            if (testDetails == null)
                return new List<TestAutomationDetail>();

            return detailsFilter switch
            {
                "success" => testDetails.Where(d => d.Success).ToList(),
                "failed" => testDetails.Where(d => !d.Success).ToList(),
                _ => testDetails
            };
        }

        private void OnFileSelected(InputFileChangeEventArgs e)
        {
            selectedFile = e.File;
        }

        private bool CanRunTests()
        {
            return selectedFile != null && selectedDatabaseId > 0 && !string.IsNullOrEmpty(selectedLlmProvider);
        }

        private async Task RunTests()
        {
            if (!CanRunTests())
            {
                toastService.ShowWarning("Please select a database, LLM provider, and file before running tests");
                return;
            }

            try
            {
                isUploading = true;

                var content = new MultipartFormDataContent();
                content.Add(new StreamContent(selectedFile.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024)), "file", selectedFile.Name);

                var response = await Http.PostAsync(
                    $"api/testautomation/upload?databaseId={selectedDatabaseId}&llmProvider={selectedLlmProvider}",
                    content
                );

                if (response.IsSuccessStatusCode)
                {
                    var fileData = await response.Content.ReadAsByteArrayAsync();
                    await JSRuntime.InvokeVoidAsync(
                        "saveAsFile",
                        $"TestResults_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                        Convert.ToBase64String(fileData)
                    );

                    toastService.ShowSuccess("Tests completed successfully. Results downloaded.");

                    // Refresh the test jobs list
                    await LoadTestJobs();

                    // Switch to history tab
                    SetActiveTab("history");
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    toastService.ShowError($"Error processing tests: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                toastService.ShowError("Error running tests: " + ex.Message);
            }
            finally
            {
                isUploading = false;
            }
        }

        private async Task DownloadTemplate()
        {
            try
            {
                var response = await Http.GetAsync("api/testautomation/template");

                if (response.IsSuccessStatusCode)
                {
                    var fileData = await response.Content.ReadAsByteArrayAsync();
                    await JSRuntime.InvokeVoidAsync(
                        "saveAsFile",
                        "TestTemplate.xlsx",
                        Convert.ToBase64String(fileData)
                    );

                    toastService.ShowSuccess("Test template downloaded");
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    toastService.ShowError($"Error downloading template: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                toastService.ShowError("Error downloading template: " + ex.Message);
            }
        }

        private async Task DownloadSampleQuestions()
        {
            try
            {
                var response = await Http.GetAsync("api/testautomation/sample-questions");

                if (response.IsSuccessStatusCode)
                {
                    var fileData = await response.Content.ReadAsByteArrayAsync();
                    await JSRuntime.InvokeVoidAsync(
                        "saveAsFile",
                        "SampleTestQuestions.xlsx",
                        Convert.ToBase64String(fileData)
                    );

                    toastService.ShowSuccess("Sample questions downloaded");
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    toastService.ShowError($"Error downloading sample questions: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                toastService.ShowError("Error downloading sample questions: " + ex.Message);
            }
        }

        private void SelectJob(TestAutomationJob job)
        {
            selectedJob = job;
            currentPage = 1; // Reset pagination
            LoadJobDetails();
        }

        private void ViewJobDetails(TestAutomationJob job)
        {
            selectedJob = job;
            currentPage = 1; // Reset pagination
            SetActiveTab("results");
            LoadJobDetails();
        }



        // Add a class to deserialize the API response with total count
        private class TestJobDetailsResponse
        {
            public List<TestAutomationDetail> Data { get; set; }
            public int TotalCount { get; set; }
        }

        // Update the LoadJobDetails method to handle the new response format
        private async Task LoadJobDetails()
        {
            if (selectedJob == null)
                return;

            try
            {
                isLoadingDetails = true;

                // Use the new response class that includes total count
                var response = await Http.GetFromJsonAsync<TestJobDetailsResponse>(
                    $"api/testautomation/jobs/{selectedJob.JobID}?pageNumber={currentPage}&pageSize={pageSize}");

                if (response != null)
                {
                    testDetails = response.Data;
                    totalDetailsCount = response.TotalCount; // Set total count from response
                }
                else
                {
                    toastService.ShowError("Error loading job details: Details is empty");
                }
            }
            catch (Exception ex)
            {
                toastService.ShowError("Error loading job details: " + ex.Message);
                testDetails = new List<TestAutomationDetail>();
                totalDetailsCount = 0;
            }
            finally
            {
                isLoadingDetails = false;
                StateHasChanged();
            }
        }

        private async Task ViewDetailComparison(TestAutomationDetail detail)
        {
            selectedDetail = detail;
            showComparisonModal = true;
            SetActiveComparisonTab("datasets"); // Default to datasets tab
            SetActiveDatasetTab("expected");

            // Load dataset comparison data
            await LoadDatasetComparison(detail.DetailID);
        }

        // These are the updated/fixed methods for TestDashboard.razor.cs specifically focused on comparison functionality

        /// <summary>
        /// Loads dataset comparison data for visualization with improved error handling
        /// </summary>
        private async Task LoadDatasetComparison(int detailId)
        {
            try
            {
                isLoadingDatasets = true;
                StateHasChanged(); // Update UI to show loading state

                // Set a timeout for the API call to prevent indefinite loading
                using var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(30)); // 30-second timeout

                var response = await Http.GetAsync($"api/testautomation/comparison/{detailId}", cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    toastService.ShowError($"Error loading comparison data: {response.StatusCode}");
                    Console.Error.WriteLine($"API Error: {errorContent}");

                    comparisonData = new DatasetComparisonResult
                    {
                        ComparisonSummary = $"Failed to load comparison data: {response.StatusCode}",
                        Expected = new List<Dictionary<string, object>>(),
                        Actual = new List<Dictionary<string, object>>()
                    };
                    return;
                }

                var responseContent = await response.Content.ReadAsStringAsync();

                try
                {
                    // Use System.Text.Json with custom options
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        NumberHandling = JsonNumberHandling.AllowReadingFromString,
                        Converters = { new DynamicDashboardCommon.Helper.DictionaryObjectConverter() }
                    };

                    // Deserialize into DatasetComparisonResult
                    var result = JsonSerializer.Deserialize<DatasetComparisonResult>(responseContent, options);

                    if (result == null)
                    {
                        toastService.ShowError("Failed to parse comparison data");
                        comparisonData = new DatasetComparisonResult
                        {
                            ComparisonSummary = "Failed to parse comparison data",
                            Expected = new List<Dictionary<string, object>>(),
                            Actual = new List<Dictionary<string, object>>()
                        };
                        return;
                    }

                    // Set the comparison data
                    comparisonData = result;

                    // Also set the data for the TestCaseComparisonView component
                    expectedDataset = result.Expected;
                    actualDataset = result.Actual;

                    // Log success and count information
                    int expectedCount = result.Expected?.Count ?? 0;
                    int actualCount = result.Actual?.Count ?? 0;
                    Console.WriteLine($"Successfully loaded {expectedCount} expected and {actualCount} actual records.");
                }
                catch (JsonException jsonEx)
                {
                    toastService.ShowError($"Error parsing comparison data: {jsonEx.Message}");
                    Console.Error.WriteLine($"JSON Error: {jsonEx}");

                    // Set empty data in case of error
                    comparisonData = new DatasetComparisonResult
                    {
                        ComparisonSummary = $"Error parsing comparison data: {jsonEx.Message}",
                        Expected = new List<Dictionary<string, object>>(),
                        Actual = new List<Dictionary<string, object>>()
                    };
                }
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Error loading dataset comparison: {ex.Message}");
                Console.Error.WriteLine($"Generic Error: {ex}");

                // Set empty data in case of error
                comparisonData = new DatasetComparisonResult
                {
                    ComparisonSummary = $"Error: {ex.Message}",
                    Expected = new List<Dictionary<string, object>>(),
                    Actual = new List<Dictionary<string, object>>()
                };
            }
            finally
            {
                isLoadingDatasets = false;
                StateHasChanged(); // Update UI to hide loading state
            }
        }

        private void CloseComparisonView()
        {
            // Close both views to ensure nothing remains open
            showComparisonView = false;
            showComparisonModal = false;

            // Reset related data
            comparisonData = new DatasetComparisonResult
            {
                Expected = new List<Dictionary<string, object>>(),
                Actual = new List<Dictionary<string, object>>(),
                ComparisonSummary = "No comparison performed"
            };
            comparisonDetail = null;
            selectedDetail = null;
            expectedDataset = null;
            actualDataset = null;

            StateHasChanged();
        }

        private async Task DownloadResults()
        {
            if (selectedJob == null)
                return;

            try
            {
                // Here you would implement the logic to download the results file
                // This could be fetching the original results file from the server
                toastService.ShowInfo("Results download functionality not implemented yet");
            }
            catch (Exception ex)
            {
                toastService.ShowError("Error downloading results: " + ex.Message);
            }
        }

        private void PreviousPage()
        {
            if (currentPage > 1)
            {
                currentPage--;
                LoadJobDetails();
            }
        }

        private void NextPage()
        {
            if (currentPage < totalPages)
            {
                currentPage++;
                LoadJobDetails();
            }
        }

        private void GoToPage(int pageNumber)
        {
            if (pageNumber >= 1 && pageNumber <= totalPages)
            {
                currentPage = pageNumber;
                LoadJobDetails();
            }
        }

        private async Task ImportJsonTestCases()
        {
            if (string.IsNullOrWhiteSpace(jsonTestCases))
            {
                toastService.ShowWarning("Please enter JSON test cases");
                return;
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var request = JsonSerializer.Deserialize<TestCasesImportRequest>(jsonTestCases, options);

                var response = await Http.PostAsJsonAsync("api/testautomation/import-json", request);

                if (response.IsSuccessStatusCode)
                {
                    var fileData = await response.Content.ReadAsByteArrayAsync();
                    await JSRuntime.InvokeVoidAsync(
                        "saveAsFile",
                        "GeneratedTestCases.xlsx",
                        Convert.ToBase64String(fileData)
                    );

                    toastService.ShowSuccess("Test cases converted to Excel template successfully");
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    toastService.ShowError($"Error importing test cases: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                toastService.ShowError("Error processing JSON: " + ex.Message);
            }
        }

        private async Task ViewDetailComparisonEnhanced(TestAutomationDetail detail)
        {
            comparisonDetail = detail;
            selectedDetail = detail; // Ensure both are set for proper state management

            // Show loading indicator
            isLoadingDatasets = true;
            StateHasChanged();

            try
            {
                // Load dataset comparison data
                await LoadDatasetComparison(detail.DetailID);

                // Show the enhanced view and hide the modal view
                showComparisonView = true;
                showComparisonModal = false;
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Error preparing comparison view: {ex.Message}");
                Console.Error.WriteLine($"View Error: {ex}");
            }
            finally
            {
                isLoadingDatasets = false;
                StateHasChanged();
            }
        }

        private void ToggleComparisonView()
        {
            if (showComparisonModal && selectedDetail != null)
            {
                // Switch from modal to enhanced view
                showComparisonModal = false;
                comparisonDetail = selectedDetail;
                showComparisonView = true;
            }
            else if (showComparisonView && comparisonDetail != null)
            {
                // Switch from enhanced view to modal
                showComparisonView = false;
                selectedDetail = comparisonDetail;
                showComparisonModal = true;
            }

            StateHasChanged();
        }



        // Helper methods
        private int GetSuccessRate(TestAutomationJob job)
        {
            return job.TotalQuestions > 0
                ? (int)Math.Round((double)job.SuccessCount / job.TotalQuestions * 100)
                : 0;
        }

        private string FormatPercent(decimal? value)
        {
            return value.HasValue ? $"{value.Value:P1}" : "N/A";
        }

        private string GetScoreClass(decimal? score)
        {
            if (!score.HasValue)
                return "error";

            return score.Value switch
            {
                >= 0.9m => "excellent",
                >= 0.7m => "good",
                >= 0.5m => "fair",
                _ => "poor"
            };
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }

    }


}