using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DynamicDashboardCommon.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Net.Http.Json;


namespace DynamicDashboardFE.Pages.Admin
{
    public partial class DataBaseMetaDataWithAnalysis : ComponentBase
    {
        [Parameter] public int? DatabaseId { get; set; }



        private List<Database> Databases = new List<Database>();
        private DatabaseSchema SchemaDetails;
        private SchemaAnalysisResult AnalysisResult;
        private Database CurrentDatabase;
        private int SelectedDatabaseId;

        private bool IsLoading;
        private bool IsAnalyzing;
        private bool IsApplying;

        private string SchemaSearch = "";
        private string ColumnFilter = "";
        private string TableFilter = "";
        private string ActiveTab = "tables";

        private HashSet<string> ExpandedTables = new HashSet<string>();
        private Dictionary<string, bool> SelectedTables = new Dictionary<string, bool>();
        private Dictionary<string, bool> SelectedColumns = new Dictionary<string, bool>();

        protected override async Task OnInitializedAsync()
        {
            try
            {
                // Load all databases
                Databases = await Http.GetFromJsonAsync<List<Database>>("api/databases");

                // Set selected database if provided in route
                if (DatabaseId.HasValue && DatabaseId.Value > 0)
                {
                    SelectedDatabaseId = DatabaseId.Value;
                    await LoadDatabaseSchema();
                }
            }
            catch (Exception ex)
            {
                toastService.ShowError("Error loading databases: " + ex.Message);
                await LogToConsole("Error: " + ex.Message);
            }
        }

        private async Task OnDatabaseChange(ChangeEventArgs e)
        {
            try
            {
                int dbId = Convert.ToInt32(e.Value);
                if (dbId != SelectedDatabaseId)
                {
                    SelectedDatabaseId = dbId;
                    AnalysisResult = null;
                    SchemaDetails = null;

                    // Update the URL
                    NavigationManager.NavigateTo($"/admin/database-metadata-json/{dbId}");

                    if (dbId > 0)
                    {
                        await LoadDatabaseSchema();
                    }
                }
            }
            catch (Exception ex)
            {
                toastService.ShowError("Error changing database: " + ex.Message);
                await LogToConsole("Error: " + ex.Message);
            }
        }

        private async Task LoadDatabaseSchema()
        {
            if (SelectedDatabaseId <= 0)
                return;

            try
            {
                IsLoading = true;

                // Get the selected database details
                CurrentDatabase = Databases.FirstOrDefault(d => d.DatabaseID == SelectedDatabaseId);

                // Get database schema
                var response = await Http.GetAsync($"api/DatabaseSchema/GetSchema/{SelectedDatabaseId}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonSchema = await response.Content.ReadFromJsonAsync<DatabaseSchema>();

                    if (jsonSchema != null && !string.IsNullOrWhiteSpace(jsonSchema.SchemaData))
                    {
                        // The schema exists - deserialize it
                        SchemaDetails = await Http.GetFromJsonAsync<DatabaseSchema>($"api/DatabaseSchema/parsed/{DatabaseId}");
                        InitializeSelectedItems();
                    }
                    else
                    {
                        // No schema exists yet - create a minimal one
                        toastService.ShowWarning("No schema found for this database");
                    }
                }
            }
            catch (Exception ex)
            {
                toastService.ShowError("Error loading schema: " + ex.Message);
                await LogToConsole("Error: " + ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RefreshSchema()
        {
            if (SelectedDatabaseId <= 0)
                return;

            try
            {
                IsLoading = true;
                toastService.ShowInfo("Refreshing schema from database...");

                // Force refresh of schema from database
                var response = await Http.PostAsync($"api/DatabaseSchema/refresh/{SelectedDatabaseId}", null);

                if (response.IsSuccessStatusCode)
                {
                    await LoadDatabaseSchema();
                    toastService.ShowSuccess("Schema refreshed successfully");
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    toastService.ShowError("Error refreshing schema: " + errorMessage);
                }
            }
            catch (Exception ex)
            {
                toastService.ShowError("Error refreshing schema: " + ex.Message);
                await LogToConsole("Error: " + ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task StartAnalysis()
        {
            if (SelectedDatabaseId <= 0 || SchemaDetails == null)
                return;

            try
            {
                IsAnalyzing = true;
                toastService.ShowInfo("Analyzing schema with AI...");

                // Call schema analysis API
                var response = await Http.GetAsync($"api/SchemaAnalysis/analyze/{SelectedDatabaseId}");

                if (response.IsSuccessStatusCode)
                {
                    AnalysisResult = await response.Content.ReadFromJsonAsync<SchemaAnalysisResult>();

                    if (AnalysisResult?.Success == true)
                    {
                        toastService.ShowSuccess("Schema analysis completed successfully");

                        // Pre-select all items
                        PreSelectAllItems();

                        // Set active tab based on what has suggestions
                        SetInitialActiveTab();
                    }
                    else
                    {
                        toastService.ShowError("Schema analysis failed: " + AnalysisResult?.ErrorMessage);
                    }
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    toastService.ShowError("Error analyzing schema: " + errorMessage);
                }
            }
            catch (Exception ex)
            {
                toastService.ShowError("Error during schema analysis: " + ex.Message);
                await LogToConsole("Error: " + ex.Message);
            }
            finally
            {
                IsAnalyzing = false;
            }
        }

        private async Task ApplyAnalysisResults()
        {
            if (AnalysisResult?.Success != true || SelectedDatabaseId <= 0)
                return;

            try
            {
                IsApplying = true;

                // Prepare data for API
                var filteredData = new SchemaAnalysisData
                {
                    TableDescriptions = FilterSelectedTableDescriptions(),
                    ColumnDescriptions = FilterSelectedColumnDescriptions(),
                    PotentialConflicts = AnalysisResult.AnalysisData.PotentialConflicts,
                    SuggestedRelationships = AnalysisResult.AnalysisData.SuggestedRelationships,
                    UnclearElements = AnalysisResult.AnalysisData.UnclearElements
                };

                // Call API to apply changes
                var response = await Http.PostAsJsonAsync($"api/SchemaAnalysis/apply/{SelectedDatabaseId}", filteredData);

                if (response.IsSuccessStatusCode)
                {
                    var success = await response.Content.ReadFromJsonAsync<bool>();

                    if (success)
                    {
                        toastService.ShowSuccess("Successfully applied schema changes");

                        // Reload schema to show changes
                        await LoadDatabaseSchema();

                        // Clear analysis results
                        AnalysisResult = null;
                    }
                    else
                    {
                        toastService.ShowError("Failed to apply schema changes");
                    }
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    toastService.ShowError("Error applying changes: " + errorMessage);
                }
            }
            catch (Exception ex)
            {
                toastService.ShowError("Error applying changes: " + ex.Message);
                await LogToConsole("Error: " + ex.Message);
            }
            finally
            {
                IsApplying = false;
            }
        }

        private async Task DismissAnalysisResults()
        {
            AnalysisResult = null;
            await Task.CompletedTask;
        }

        private void SetActiveTab(string tabName)
        {
            ActiveTab = tabName;
        }

        private void ToggleTableExpansion(string tableId)
        {
            if (ExpandedTables.Contains(tableId))
            {
                ExpandedTables.Remove(tableId);
            }
            else
            {
                ExpandedTables.Add(tableId);
            }
        }

        private List<TableSchema> GetFilteredSchemaTables()
        {
            if (SchemaDetails?.Tables == null)
                return new List<TableSchema>();

            if (string.IsNullOrWhiteSpace(SchemaSearch))
                return SchemaDetails.Tables;

            return SchemaDetails.Tables.Where(t =>
                t.DBName.Contains(SchemaSearch, StringComparison.OrdinalIgnoreCase) ||
                (t.FriendlyName?.Contains(SchemaSearch, StringComparison.OrdinalIgnoreCase) == true) ||
                (t.Description?.Contains(SchemaSearch, StringComparison.OrdinalIgnoreCase) == true) ||
                t.Columns.Any(c =>
                    c.DBName.Contains(SchemaSearch, StringComparison.OrdinalIgnoreCase) ||
                    (c.FriendlyName?.Contains(SchemaSearch, StringComparison.OrdinalIgnoreCase) == true) ||
                    (c.Description?.Contains(SchemaSearch, StringComparison.OrdinalIgnoreCase) == true)
                )
            ).ToList();
        }

        private List<ColumnDescription> GetFilteredColumns()
        {
            if (AnalysisResult?.AnalysisData?.ColumnDescriptions == null)
                return new List<ColumnDescription>();

            var filteredColumns = AnalysisResult.AnalysisData.ColumnDescriptions.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(TableFilter))
            {
                filteredColumns = filteredColumns.Where(c =>
                    c.TableName.Equals(TableFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(ColumnFilter))
            {
                filteredColumns = filteredColumns.Where(c =>
                    c.ColumnName.Contains(ColumnFilter, StringComparison.OrdinalIgnoreCase) ||
                    c.SuggestedName.Contains(ColumnFilter, StringComparison.OrdinalIgnoreCase) ||
                    c.SuggestedDescription.Contains(ColumnFilter, StringComparison.OrdinalIgnoreCase));
            }

            return filteredColumns.ToList();
        }

        private List<string> GetDistinctTables()
        {
            if (AnalysisResult?.AnalysisData?.ColumnDescriptions == null)
                return new List<string>();

            return AnalysisResult.AnalysisData.ColumnDescriptions
                .Select(c => c.TableName)
                .Distinct()
                .OrderBy(t => t)
                .ToList();
        }

        private int GetTotalColumns()
        {
            if (SchemaDetails?.Tables == null)
                return 0;

            return SchemaDetails.Tables.Sum(t => t.Columns?.Count ?? 0);
        }

        private string GetColumnKey(ColumnDescription column)
        {
            return $"{column.TableName}_{column.ColumnName}";
        }

        private void InitializeSelectedItems()
        {
            SelectedTables.Clear();
            SelectedColumns.Clear();
        }

        private void PreSelectAllItems()
        {
            if (AnalysisResult?.AnalysisData == null)
                return;

            // Pre-select all tables
            if (AnalysisResult.AnalysisData.TableDescriptions != null)
            {
                foreach (var table in AnalysisResult.AnalysisData.TableDescriptions)
                {
                    SelectedTables[table.TableName] = true;
                }
            }

            // Pre-select all columns
            if (AnalysisResult.AnalysisData.ColumnDescriptions != null)
            {
                foreach (var column in AnalysisResult.AnalysisData.ColumnDescriptions)
                {
                    SelectedColumns[GetColumnKey(column)] = true;
                }
            }
        }

        private List<TableDescription> FilterSelectedTableDescriptions()
        {
            if (AnalysisResult?.AnalysisData?.TableDescriptions == null)
                return new List<TableDescription>();

            return AnalysisResult.AnalysisData.TableDescriptions
                .Where(t => SelectedTables.ContainsKey(t.TableName) && SelectedTables[t.TableName])
                .ToList();
        }

        private List<ColumnDescription> FilterSelectedColumnDescriptions()
        {
            if (AnalysisResult?.AnalysisData?.ColumnDescriptions == null)
                return new List<ColumnDescription>();

            return AnalysisResult.AnalysisData.ColumnDescriptions
                .Where(c => SelectedColumns.ContainsKey(GetColumnKey(c)) && SelectedColumns[GetColumnKey(c)])
                .ToList();
        }

        private void SetInitialActiveTab()
        {
            if (AnalysisResult?.AnalysisData == null)
                return;

            if (AnalysisResult.AnalysisData.TableDescriptions?.Count > 0)
            {
                ActiveTab = "tables";
            }
            else if (AnalysisResult.AnalysisData.ColumnDescriptions?.Count > 0)
            {
                ActiveTab = "columns";
            }
            else if (AnalysisResult.AnalysisData.PotentialConflicts?.Count > 0)
            {
                ActiveTab = "conflicts";
            }
            else if (AnalysisResult.AnalysisData.SuggestedRelationships?.Count > 0)
            {
                ActiveTab = "relationships";
            }
            else if (AnalysisResult.AnalysisData.UnclearElements?.Count > 0)
            {
                ActiveTab = "unclear";
            }
        }

        private async Task LogToConsole(string message)
        {
            await JSRuntime.InvokeVoidAsync("console.log", message);
        }
    }
}