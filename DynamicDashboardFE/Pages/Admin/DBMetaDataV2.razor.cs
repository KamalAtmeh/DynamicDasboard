using Microsoft.AspNetCore.Components;
using DynamicDashboardCommon.Models;

using System.Threading.Tasks;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Reflection;
using Blazored.Toast.Services;
using static System.Net.WebRequestMethods;

namespace DynamicDashboardFE.Pages.Admin
{
    public partial class DBMetaDataV2 : ComponentBase
    {




        #region Variables

        // State variables
        private string activeTab = "tables";
        private string analysisTab = "tables";
        private bool isLoading = true;
        private bool isAnalyzing = false;
        private bool isApplying = false;
        private string loadingMessage = "Loading database metadata...";

        // Loading overlay variables
        private string analysisLoadingMessage = "Analyzing Schema...";
        private int analysisProgress = 0;
        private int analysisStep = 0;
        private CancellationTokenSource analysisCancellationToken;

        // Data objects
        private List<Database> databases;
        private Database selectedDatabase;
        private DatabaseSchema schemaObj;
        private TableSchema selectedTable;
        private ColumnSchema selectedColumn;
        private ColumnDescription selectedColumnAnalysis;
        private SchemaAnalysisResult analysisResult;
        private List<RelationshipSchema> relationships;

        // Search and filter terms
        private string tableSearchTerm = string.Empty;
        private string columnSearchTerm = string.Empty;
        private string columnFilter = "all";
        private string relationshipSearchTerm = string.Empty;
        private string relationshipFilter = "all";

        // Modal states
        private bool isColumnSuggestionModalOpen = false;
        private bool isColumnSynonymsModalOpen = false;
        private ColumnSchema editingColumnSynonyms;
        private string columnSynonymsInput = string.Empty;
        private string tableSynonymsInput = string.Empty;

        // Relationship editing
        private RelationshipSchema editingRelationship;
        private bool isNewRelationship = false;
        private Dictionary<string, string> sourceColumnOptions = new Dictionary<string, string>();
        private Dictionary<string, string> targetColumnOptions = new Dictionary<string, string>();
        private bool isSavingRelationship = false;
        private string relationshipErrorMessage = string.Empty;

        //Term Mapping
        private bool isSuggestingTerms = false;
        private TermMapping editingTermMapping;
        private bool isTermMappingModalOpen = false;
        private string termSearchTerm = string.Empty;
        private string termTypeFilter = "all";
        private bool isFormulaModalOpen = false;
        private bool isEditingFormula = false;
        private TermMapping currentFormulaTerm;
        private string formulaText;
        private bool isValidatingFormula = false;
        private QueryValidationResult formulaValidationResult;
        private bool isSavingAllSuggestions = false;


        private string synonymInput = string.Empty;
        private TermMappingDependency editingDependency;
        private bool isDependencyModalOpen = false;

        // Add to variables region
        private HashSet<string> appliedTableSuggestions = new HashSet<string>();
        private HashSet<string> appliedColumnSuggestions = new HashSet<string>();

        #endregion

        #region Properties

        [Parameter]
        public int DatabaseId { get; set; }

        private IEnumerable<TermMapping> FilteredTermMappings
        {
            get
            {
                if (schemaObj?.TermMappings == null)
                    return Enumerable.Empty<TermMapping>();

                var filtered = schemaObj.TermMappings.AsEnumerable();

                // Apply search filter
                if (!string.IsNullOrEmpty(termSearchTerm))
                {
                    filtered = filtered.Where(t =>
                        t.BusinessTerm.Contains(termSearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        t.Description.Contains(termSearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        (t.Synonyms?.Any(s => s.Contains(termSearchTerm, StringComparison.OrdinalIgnoreCase)) ?? false)
                    );
                }

                // Apply type filter
                if (termTypeFilter != "all")
                {
                    filtered = filtered.Where(t => t.Type.ToString() == termTypeFilter);
                }

                return filtered.OrderBy(t => t.BusinessTerm);
            }
        }

        #endregion

        protected override async Task OnInitializedAsync()
        {
            await ClearCacheAsync();
            await LoadDatabases();
            await LoadSelectedDatabase();
        }



        private async Task LoadDatabases()
        {
            try
            {
                databases = await Http.GetFromJsonAsync<List<Database>>("api/databases");
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Error loading databases: {ex.Message + ", " + ex.StackTrace}");
            }
        }

        private async Task LoadSelectedDatabase()
        {
            isLoading = true;
            loadingMessage = "Loading database metadata...";

            try
            {
                selectedDatabase = await Http.GetFromJsonAsync<Database>($"api/databases/{DatabaseId}");

                // Load schema
                await LoadDatabaseSchema();

                // Select first table by default
                if (schemaObj?.Tables?.Any() == true)
                {
                    selectedTable = schemaObj.Tables.First();
                }
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Error loading database: {ex.Message + ", " + ex.StackTrace + ", " + ex.StackTrace}");
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task LoadDatabaseSchema(bool useCache = false)
        {
            try
            {
                // Get parsed schema
                if (useCache)
                {
                    schemaObj = await Http.GetFromJsonAsync<DatabaseSchema>($"api/databaseschema/parsed/{DatabaseId}/1");
                }
                else
                {
                    schemaObj = await Http.GetFromJsonAsync<DatabaseSchema>($"api/databaseschema/parsed/{DatabaseId}/0");
                }

                // Load relationships
                relationships = schemaObj?.Relationships ?? new List<RelationshipSchema>();

            }
            catch (Exception ex)
            {
                toastService.ShowError($"Error loading schema: {ex.Message + ", " + ex.StackTrace}");
            }
        }

        private async Task RefreshSchema()
        {
            isLoading = true;
            loadingMessage = "Refreshing database schema...";

            try
            {
                await Http.PostAsync($"api/databaseschema/refresh/{DatabaseId}", null);
                await LoadDatabaseSchema(false);
                toastService.ShowSuccess("Schema refreshed successfully.");
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Error refreshing schema: {ex.Message + ", " + ex.StackTrace}");
            }
            finally
            {
                isLoading = false;
            }
        }

        private void SetActiveTab(string tab)
        {
            activeTab = tab;
        }

        private void SelectTable(TableSchema table)
        {
            selectedTable = table;
            activeTab = "tables"; // Ensure we're showing the tables tab
        }

        // Table operations
        private async Task SaveTableChanges()
        {
            if (selectedTable == null)
                return;

            try
            {
                // Create a minimal TableUpdateDto with only the necessary fields
                var tableUpdate = new TableSchema
                {
                    ID = selectedTable.ID,
                    FriendlyName = selectedTable.FriendlyName,
                    Description = selectedTable.Description,
                    Synonyms = selectedTable.Synonyms,
                    IsActive = selectedTable.IsActive

                };

                // Use a dedicated endpoint for updating just the table
                var result = await Http.PutAsync($"api/databaseschema/UpdateTableDetailsByTableID/{DatabaseId}/{selectedTable.ID}",
                                   JsonContent.Create(tableUpdate));

                var success = await result.Content.ReadFromJsonAsync<bool>();
                if (result.IsSuccessStatusCode && success)
                {

                    toastService.ShowSuccess("Table changes saved successfully.");
                }
                else
                {
                    toastService.ShowError("Error saving table changes.");
                }
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Error saving table changes: {ex.Message + ", " + ex.StackTrace}");
            }
        }

        private async Task UpdateTableActiveStatus(object value)
        {
            if (selectedTable == null || !(value is bool))
                toastService.ShowError("Error Happened, Please try again");

            bool isActive = (bool)value;
            try
            {
                await Http.PutAsync($"api/databaseschema/tables/{DatabaseId}/{selectedTable.ID}/active",
                                    JsonContent.Create(isActive));

                selectedTable.IsActive = isActive;
                toastService.ShowSuccess($"Table {(isActive ? "activated" : "deactivated")} successfully.");
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Error updating table status: {ex.Message + ", " + ex.StackTrace}");
            }
        }

        private void UpdateTableSynonyms()
        {
            if (selectedTable == null || string.IsNullOrWhiteSpace(tableSynonymsInput))
                return;

            // Initialize synonyms list if null
            if (selectedTable.Synonyms == null)
                selectedTable.Synonyms = new List<string>();

            // Split input by commas and add new synonyms
            var newSynonyms = tableSynonymsInput.Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            foreach (var synonym in newSynonyms)
            {
                if (!selectedTable.Synonyms.Contains(synonym, StringComparer.OrdinalIgnoreCase))
                {
                    selectedTable.Synonyms.Add(synonym);
                }
            }

            tableSynonymsInput = string.Empty;
        }

        private void RemoveTableSynonym(string synonym)
        {
            if (selectedTable?.Synonyms == null)
                return;

            selectedTable.Synonyms.Remove(synonym);
        }

        // Column operations
        private async Task SaveColumnChanges()
        {
            if (selectedTable == null || selectedTable.Columns == null)
                return;

            try
            {
                // Create a list of minimal column update DTOs
                var columnUpdates = selectedTable.Columns.Select(c => new ColumnSchema
                {
                    ID = c.ID,
                    FriendlyName = c.FriendlyName,
                    Description = c.Description,
                    IsLookup = c.IsLookup,
                    Synonyms = c.Synonyms,
                    IsActive = c.IsActive,
                    IsNullable = c.IsNullable,
                    IsPrimaryKey = c.IsPrimaryKey,
                    DataType = c.DataType

                }).ToList();

                // Send only the column updates in a batch
                await Http.PutAsync($"api/databaseschema/UpdateColumnsDetailsByColumnID/{DatabaseId}/{selectedTable.ID}",
                JsonContent.Create(columnUpdates));

                toastService.ShowSuccess("Column changes saved successfully.");
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Error saving column changes: {ex.Message + ", " + ex.StackTrace}");
            }
        }

        private async Task ToggleColumnActive(ColumnSchema column)
        {
            if (column == null || selectedTable == null)
                return;

            try
            {
                bool newStatus = !column.IsActive;
                await Http.PutAsync($"api/databaseschema/columns/{DatabaseId}/{selectedTable.ID}/{column.ID}/active",
                                    JsonContent.Create(newStatus));

                column.IsActive = newStatus;
                toastService.ShowSuccess($"Column {(newStatus ? "activated" : "deactivated")} successfully.");
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Error updating column status: {ex.Message + ", " + ex.StackTrace}");
            }
        }

        private void AddEditColumnSynonyms(ColumnSchema column)
        {
            editingColumnSynonyms = column;
            columnSynonymsInput = string.Empty;
            isColumnSynonymsModalOpen = true;
        }

        private void AddColumnSynonym()
        {
            if (editingColumnSynonyms == null || string.IsNullOrWhiteSpace(columnSynonymsInput))
                return;

            // Initialize synonyms list if null
            if (editingColumnSynonyms.Synonyms == null)
                editingColumnSynonyms.Synonyms = new List<string>();

            // Add new synonym if not already present
            if (!editingColumnSynonyms.Synonyms.Contains(columnSynonymsInput, StringComparer.OrdinalIgnoreCase))
            {
                editingColumnSynonyms.Synonyms.Add(columnSynonymsInput);
            }

            columnSynonymsInput = string.Empty;
        }

        private void RemoveColumnSynonym(string synonym)
        {
            if (editingColumnSynonyms?.Synonyms == null)
                return;

            editingColumnSynonyms.Synonyms.Remove(synonym);
        }

        private void SaveColumnSynonyms()
        {
            CloseColumnSynonymsModal();
        }

        private async Task UpdateColumnOptions()
        {
            // This is called when the table selection changes
            StateHasChanged();
        }

        private void CloseColumnSynonymsModal()
        {
            isColumnSynonymsModalOpen = false;
            editingColumnSynonyms = null;
        }

        // Column suggestion modal
        private void ShowColumnSuggestion(ColumnSchema column, ColumnDescription analysis)
        {
            selectedColumn = column;
            selectedColumnAnalysis = analysis;
            isColumnSuggestionModalOpen = true;
        }

        private void CloseColumnSuggestionModal()
        {
            isColumnSuggestionModalOpen = false;
            selectedColumn = null;
            selectedColumnAnalysis = null;
        }

        private void ApplyColumnSuggestion()
        {
            if (selectedColumn != null && selectedColumnAnalysis != null)
            {
                selectedColumn.FriendlyName = selectedColumnAnalysis.SuggestedName;
                selectedColumn.Description = selectedColumnAnalysis.SuggestedDescription;
                selectedColumn.IsLookup = selectedColumnAnalysis.IsLookupColumn;

                CloseColumnSuggestionModal();
            }
        }

        // Relationship operations
        private void CreateNewRelationship()
        {
            isNewRelationship = true;
            var sourceDetails = new RelationshipDetails { TableID = "", ColumnID = "" };
            var targetDetails = new RelationshipDetails { TableID = "", ColumnID = "" };

            editingRelationship = new RelationshipSchema
            {
                ID = Guid.NewGuid().ToString(),
                Name = "",
                Type = "One-to-Many", // Default type
                Source = sourceDetails,
                Target = targetDetails,
                Enforced = false,
                IsActive = true
            };

            relationshipErrorMessage = string.Empty;
        }

        private void EditRelationship(RelationshipSchema relationship)
        {
            isNewRelationship = false;

            // Create a new instance to avoid modifying the original
            editingRelationship = new RelationshipSchema
            {
                ID = relationship.ID,
                Name = relationship.Name,
                Type = relationship.Type,
                Source = new RelationshipDetails
                {
                    TableID = relationship.Source?.TableID,
                    TableName = relationship.Source?.TableName,
                    ColumnID = relationship.Source?.ColumnID,
                    ColumnName = relationship.Source?.ColumnName
                },
                Target = new RelationshipDetails
                {
                    TableID = relationship.Target?.TableID,
                    TableName = relationship.Target?.TableName,
                    ColumnID = relationship.Target?.ColumnID,
                    ColumnName = relationship.Target?.ColumnName
                },
                Enforced = relationship.Enforced,
                IsActive = relationship.IsActive
            };

            relationshipErrorMessage = string.Empty;

            // Load column options
            UpdateSourceColumnOptions();
            UpdateTargetColumnOptions();
        }

        private void CloseRelationshipEditor()
        {
            editingRelationship = null;
            sourceColumnOptions.Clear();
            targetColumnOptions.Clear();
            relationshipErrorMessage = string.Empty;
        }

        private async Task SaveRelationship()
        {
            if (editingRelationship == null)
                return;

            // Validate required fields
            if (string.IsNullOrEmpty(editingRelationship.Source?.TableID) ||
                string.IsNullOrEmpty(editingRelationship.Source?.ColumnID) ||
                string.IsNullOrEmpty(editingRelationship.Target?.TableID) ||
                string.IsNullOrEmpty(editingRelationship.Target?.ColumnID))
            {
                relationshipErrorMessage = "Please select source table, source column, target table, and target column.";
                return;
            }

            if (string.IsNullOrEmpty(editingRelationship.Type))
            {
                relationshipErrorMessage = "Please select a relationship type.";
                return;
            }

            try
            {
                isSavingRelationship = true;
                relationshipErrorMessage = string.Empty;

                // Ensure relationship has all required fields
                if (editingRelationship.Metadata == null)
                {
                    editingRelationship.Metadata = new RelationshipMetadata
                    {
                        Confidence = 1.0,
                        DiscoveredAt = DateTime.UtcNow,
                        LastValidated = DateTime.UtcNow
                    };
                }

                // Clone and update schema
                var updatedSchema = CloneSchema(schemaObj);
                UpdateRelationshipNames(updatedSchema, editingRelationship);

                int relationshipIndex = updatedSchema.Relationships.FindIndex(r => r.ID == editingRelationship.ID);
                if (relationshipIndex >= 0)
                {
                    updatedSchema.Relationships[relationshipIndex] = editingRelationship;
                }
                else
                {
                    updatedSchema.Relationships.Add(editingRelationship);
                }

                // Serialize ONLY the inner schema content
                var jsonOptions = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = null,
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                var schemaDataContent = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Version = updatedSchema.Version,
                    Tables = updatedSchema.Tables,
                    Relationships = updatedSchema.Relationships,
                    TermMappings = updatedSchema.TermMappings,
                    AnalysisResults = updatedSchema.AnalysisResults
                }, jsonOptions);

                // Send ONLY SchemaData - the backend will handle everything else
                var payload = new { SchemaData = schemaDataContent };

                var response = await Http.PutAsJsonAsync($"api/databaseschema/UpdateByDatabaseId/{DatabaseId}", payload);

                if (response.IsSuccessStatusCode)
                {
                    await LoadDatabaseSchema(false);
                    toastService.ShowSuccess($"Relationship {(isNewRelationship ? "created" : "updated")} successfully.");
                    CloseRelationshipEditor();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    relationshipErrorMessage = $"Server error: {response.StatusCode}";
                    toastService.ShowError($"Failed to save: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                relationshipErrorMessage = $"Error: {ex.Message + ", " + ex.StackTrace}";
                toastService.ShowError($"Error: {ex.Message + ", " + ex.StackTrace}");
            }
            finally
            {
                isSavingRelationship = false;
            }
        }
        private void UpdateRelationshipNames(DatabaseSchema schema, RelationshipSchema relationship)
        {
            // Set table and column names based on IDs
            var sourceTable = schema.Tables.FirstOrDefault(t => t.ID == relationship.Source?.TableID);
            if (sourceTable != null)
            {
                relationship.Source.TableName = !string.IsNullOrEmpty(sourceTable.FriendlyName) ?
                    sourceTable.FriendlyName : sourceTable.DBName;

                var sourceColumn = sourceTable.Columns?.FirstOrDefault(c => c.ID == relationship.Source?.ColumnID);
                if (sourceColumn != null)
                {
                    relationship.Source.ColumnName = !string.IsNullOrEmpty(sourceColumn.FriendlyName) ?
                        sourceColumn.FriendlyName : sourceColumn.DBName;
                }
            }

            var targetTable = schema.Tables.FirstOrDefault(t => t.ID == relationship.Target?.TableID);
            if (targetTable != null)
            {
                relationship.Target.TableName = !string.IsNullOrEmpty(targetTable.FriendlyName) ?
                    targetTable.FriendlyName : targetTable.DBName;

                var targetColumn = targetTable.Columns?.FirstOrDefault(c => c.ID == relationship.Target?.ColumnID);
                if (targetColumn != null)
                {
                    relationship.Target.ColumnName = !string.IsNullOrEmpty(targetColumn.FriendlyName) ?
                        targetColumn.FriendlyName : targetColumn.DBName;
                }
            }
        }

        private async Task UpdateSourceColumnOptions()
        {
            sourceColumnOptions = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(editingRelationship?.Source?.TableID))
                return;

            var sourceTable = schemaObj.Tables.FirstOrDefault(t => t.ID == editingRelationship.Source.TableID);
            if (sourceTable?.Columns != null)
            {
                foreach (var column in sourceTable.Columns.Where(c => c.IsActive))
                {
                    sourceColumnOptions[column.ID] = !string.IsNullOrEmpty(column.FriendlyName) ?
                        $"{column.FriendlyName} ({column.DBName})" : column.DBName;
                }
            }
        }

        private async Task UpdateTargetColumnOptions()
        {
            targetColumnOptions = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(editingRelationship?.Target?.TableID))
                return;

            var targetTable = schemaObj.Tables.FirstOrDefault(t => t.ID == editingRelationship.Target.TableID);
            if (targetTable?.Columns != null)
            {
                foreach (var column in targetTable.Columns.Where(c => c.IsActive))
                {
                    targetColumnOptions[column.ID] = !string.IsNullOrEmpty(column.FriendlyName) ?
                        $"{column.FriendlyName} ({column.DBName})" : column.DBName;
                }
            }
        }

        private async Task DeleteRelationship(string relationshipId)
        {
            if (string.IsNullOrEmpty(relationshipId))
                return;

            bool confirmed = await JSRuntime.InvokeAsync<bool>("confirm", "Are you sure you want to delete this relationship?");
            if (!confirmed)
                return;

            try
            {
                var updatedSchema = CloneSchema(schemaObj);
                updatedSchema.Relationships.RemoveAll(r => r.ID == relationshipId);

                var jsonOptions = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = null,
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                var schemaDataContent = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Version = updatedSchema.Version,
                    Tables = updatedSchema.Tables,
                    Relationships = updatedSchema.Relationships,
                    TermMappings = updatedSchema.TermMappings,
                    AnalysisResults = updatedSchema.AnalysisResults
                }, jsonOptions);

                // Send ONLY SchemaData
                var payload = new { SchemaData = schemaDataContent };

                var response = await Http.PutAsJsonAsync($"api/databaseschema/UpdateByDatabaseId/{DatabaseId}", payload);

                if (response.IsSuccessStatusCode)
                {
                    await LoadDatabaseSchema(false);
                    toastService.ShowSuccess("Relationship deleted successfully.");
                }
                else
                {
                    toastService.ShowError("Failed to delete relationship.");
                }
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Error: {ex.Message + ", " + ex.StackTrace}");
            }
        }

        private async Task ToggleRelationshipActive(RelationshipSchema relationship)
        {
            if (relationship == null)
                return;

            try
            {
                bool newStatus = !relationship.IsActive;
                await Http.PutAsync($"api/databaseschema/relationships/{DatabaseId}/{relationship.ID}/active",
                                    JsonContent.Create(newStatus));

                relationship.IsActive = newStatus;
                toastService.ShowSuccess($"Relationship {(newStatus ? "activated" : "deactivated")} successfully.");
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Error updating relationship status: {ex.Message + ", " + ex.StackTrace}");
            }
        }

        private async Task RunSchemaAnalysis()
        {
            // Reset applied states for new analysis
            appliedTableSuggestions.Clear();
            appliedColumnSuggestions.Clear();

            if (DatabaseId <= 0)
            {
                toastService.ShowWarning("Please select a database first.");
                return;
            }

            try
            {
                isAnalyzing = true;
                StateHasChanged();

                // Call the CORRECT endpoint
                var response = await Http.GetFromJsonAsync<SchemaAnalysisResult>(
                    $"api/SchemaAnalysis/AnalyzeDatabaseSchema/{DatabaseId}");

                Console.WriteLine($"Response Success: {response?.Success}");
                Console.WriteLine($"Response Error: {response?.ErrorMessage}");
                Console.WriteLine($"Tables Count: {response?.AnalysisData?.TableDescriptions?.Count}");
                Console.WriteLine($"Columns Count: {response?.AnalysisData?.ColumnDescriptions?.Count}");

                // Assign result directly (same as original)
                analysisResult = response;

                if (response?.Success == true)
                {
                    toastService.ShowSuccess("Schema analysis completed successfully!");

                    // Switch to analysis tab to show results
                    activeTab = "analysis";
                    analysisTab = "tables";
                }
                else
                {
                    toastService.ShowError($"Analysis failed: {response?.ErrorMessage ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Error analyzing schema: {ex.Message}");
                Console.WriteLine($"Exception: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
            }
            finally
            {
                isAnalyzing = false;
                StateHasChanged(); // Force UI refresh
            }
        }

        private async Task SimulateAnalysisProgress(CancellationToken token)
        {
            try
            {
                var messages = new[]
                {
            "Scanning table structures...",
            "Analyzing column data types...",
            "Identifying naming patterns...",
            "Evaluating relationships...",
            "Processing metadata...",
            "Generating intelligent suggestions...",
            "Almost there..."
        };

                int messageIndex = 0;

                while (!token.IsCancellationRequested && analysisProgress < 85)
                {
                    await Task.Delay(2000, token);

                    if (token.IsCancellationRequested) break;

                    // Increment progress (slow down as we get higher)
                    if (analysisProgress < 30)
                        analysisProgress += 8;
                    else if (analysisProgress < 60)
                        analysisProgress += 5;
                    else if (analysisProgress < 85)
                        analysisProgress += 3;

                    // Update message periodically
                    if (analysisStep == 2 && messageIndex < messages.Length)
                    {
                        analysisLoadingMessage = messages[messageIndex];
                        messageIndex++;
                    }

                    StateHasChanged();
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelled
            }
        }

        private void CancelAnalysis()
        {
            analysisCancellationToken?.Cancel();
            isAnalyzing = false;
            analysisProgress = 0;
            analysisStep = 0;
            StateHasChanged();
            toastService.ShowInfo("Analysis cancelled.");
        }




        private async Task ApplyAllSuggestions()
        {
            if (analysisResult?.AnalysisData == null)
                return;

            isApplying = true;

            try
            {
                var result = await Http.PostAsJsonAsync($"api/SchemaAnalysis/ApplySchemaAnalysisResults/{DatabaseId}", analysisResult.AnalysisData);

                if (result.IsSuccessStatusCode)
                {
                    await LoadDatabaseSchema();
                    toastService.ShowSuccess("All suggestions applied successfully.");
                }
                else
                {
                    toastService.ShowError("Failed to apply suggestions.");
                }
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Error applying suggestions: {ex.Message + ", " + ex.StackTrace}");
            }
            finally
            {
                isApplying = false;
            }
        }

        // Suggestions and conflicts
        private async Task ApplyTableSuggestion(TableDescription suggestion)
        {
            if (suggestion == null)
                return;

            try
            {
                var table = schemaObj.Tables.FirstOrDefault(t =>
                    t.DBName.Equals(suggestion.TableName, StringComparison.OrdinalIgnoreCase));

                if (table == null)
                    return;

                // Update the table with suggested values
                table.FriendlyName = suggestion.SuggestedName;
                table.Description = suggestion.SuggestedDescription;

                // If this is the selected table, update the UI model as well
                if (selectedTable?.ID == table.ID)
                {
                    selectedTable.FriendlyName = suggestion.SuggestedName;
                    selectedTable.Description = suggestion.SuggestedDescription;
                }

                // Mark as applied
                appliedTableSuggestions.Add(suggestion.TableName);

                // REMOVED: "Don't forget to save" message
                toastService.ShowSuccess($"Applied suggestion for '{suggestion.TableName}'");

                StateHasChanged();
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Error applying suggestion: {ex.Message}");
            }
        }

        private async Task ApplyColumnSuggestion(ColumnDescription suggestion)
        {
            if (suggestion == null)
                return;

            try
            {
                var table = schemaObj.Tables.FirstOrDefault(t =>
                    t.DBName.Equals(suggestion.TableName, StringComparison.OrdinalIgnoreCase));

                if (table?.Columns == null)
                    return;

                var column = table.Columns.FirstOrDefault(c =>
                    c.DBName.Equals(suggestion.ColumnName, StringComparison.OrdinalIgnoreCase));

                if (column == null)
                    return;

                column.FriendlyName = suggestion.SuggestedName;
                column.Description = suggestion.SuggestedDescription;
                column.IsLookup = suggestion.IsLookupColumn;

                // Mark as applied
                var key = $"{suggestion.TableName}_{suggestion.ColumnName}";
                appliedColumnSuggestions.Add(key);

                toastService.ShowSuccess($"Applied suggestion for '{suggestion.ColumnName}'");

                StateHasChanged();
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Error applying suggestion: {ex.Message}");
            }
        }

        private async Task ApplyConflictResolution(string type, ConflictItem item)
        {
            if (item == null)
                return;

            try
            {
                if (type == "Table")
                {
                    var table = schemaObj.Tables.FirstOrDefault(t =>
                        t.DBName.Equals(item.Name, StringComparison.OrdinalIgnoreCase));

                    if (table != null)
                    {
                        table.FriendlyName = item.SuggestedResolution;
                        toastService.ShowSuccess("Table conflict resolution applied. Don't forget to save your changes.");
                    }
                }
                else if (type == "Column")
                {
                    var table = schemaObj.Tables.FirstOrDefault(t =>
                        t.DBName.Equals(item.TableName, StringComparison.OrdinalIgnoreCase));

                    if (table?.Columns != null)
                    {
                        var column = table.Columns.FirstOrDefault(c =>
                            c.DBName.Equals(item.Name, StringComparison.OrdinalIgnoreCase));

                        if (column != null)
                        {
                            column.FriendlyName = item.SuggestedResolution;
                            toastService.ShowSuccess("Column conflict resolution applied. Don't forget to save your changes.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Error applying conflict resolution: {ex.Message + ", " + ex.StackTrace}");
            }
        }

        private async Task ApplyUnclearElementSuggestion(UnclearElement element)
        {
            if (element == null)
                return;

            try
            {
                if (element.Type == "Table")
                {
                    var table = schemaObj.Tables.FirstOrDefault(t =>
                        t.DBName.Equals(element.Name, StringComparison.OrdinalIgnoreCase));

                    if (table != null)
                    {
                        table.Description = element.Suggestion;
                        toastService.ShowSuccess("Table description updated. Don't forget to save your changes.");
                    }
                }
                else if (element.Type == "Column")
                {
                    var table = schemaObj.Tables.FirstOrDefault(t =>
                        t.DBName.Equals(element.TableName, StringComparison.OrdinalIgnoreCase));

                    if (table?.Columns != null)
                    {
                        var column = table.Columns.FirstOrDefault(c =>
                            c.DBName.Equals(element.Name, StringComparison.OrdinalIgnoreCase));

                        if (column != null)
                        {
                            column.Description = element.Suggestion;
                            toastService.ShowSuccess("Column description updated. Don't forget to save your changes.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Error applying suggestion: {ex.Message + ", " + ex.StackTrace}");
            }
        }

        private async Task AddSuggestedRelationship(SuggestedRelationship suggestion)
        {
            if (suggestion == null)
                return;

            try
            {
                // Find source table and column
                var sourceTable = schemaObj.Tables.FirstOrDefault(t =>
                    t.DBName.Equals(suggestion.SourceTable?.TableName, StringComparison.OrdinalIgnoreCase));

                if (sourceTable?.Columns == null)
                {
                    toastService.ShowError("Source table not found.");
                    return;
                }

                var sourceColumn = sourceTable.Columns.FirstOrDefault(c =>
                    c.DBName.Equals(suggestion.SourceTable?.ColumnName, StringComparison.OrdinalIgnoreCase));

                if (sourceColumn == null)
                {
                    toastService.ShowError("Source column not found.");
                    return;
                }

                // Find target table and column
                var targetTable = schemaObj.Tables.FirstOrDefault(t =>
                    t.DBName.Equals(suggestion.TargetTable?.TableName, StringComparison.OrdinalIgnoreCase));

                if (targetTable?.Columns == null)
                {
                    toastService.ShowError("Target table not found.");
                    return;
                }

                var targetColumn = targetTable.Columns.FirstOrDefault(c =>
                    c.DBName.Equals(suggestion.TargetTable?.ColumnName, StringComparison.OrdinalIgnoreCase));

                if (targetColumn == null)
                {
                    toastService.ShowError("Target column not found.");
                    return;
                }

                // Create a new relationship
                var newRelationship = new RelationshipSchema
                {
                    ID = Guid.NewGuid().ToString(),
                    Name = $"FK_{sourceTable.DBName}_{sourceColumn.DBName}_TO_{targetTable.DBName}_{targetColumn.DBName}",
                    Type = NormalizeRelationshipType(suggestion.RelationshipType),
                    Source = new RelationshipDetails
                    {
                        TableID = sourceTable.ID,
                        TableName = !string.IsNullOrEmpty(sourceTable.FriendlyName) ? sourceTable.FriendlyName : sourceTable.DBName,
                        ColumnID = sourceColumn.ID,
                        ColumnName = !string.IsNullOrEmpty(sourceColumn.FriendlyName) ? sourceColumn.FriendlyName : sourceColumn.DBName
                    },
                    Target = new RelationshipDetails
                    {
                        TableID = targetTable.ID,
                        TableName = !string.IsNullOrEmpty(targetTable.FriendlyName) ? targetTable.FriendlyName : targetTable.DBName,
                        ColumnID = targetColumn.ID,
                        ColumnName = !string.IsNullOrEmpty(targetColumn.FriendlyName) ? targetColumn.FriendlyName : targetColumn.DBName
                    },
                    Enforced = false,
                    IsActive = true,
                    Metadata = new RelationshipMetadata
                    {
                        Confidence = suggestion.Confidence,
                        DiscoveredAt = DateTime.UtcNow,
                        LastValidated = DateTime.UtcNow
                    }
                };

                var updatedSchema = CloneSchema(schemaObj);
                updatedSchema.Relationships.Add(newRelationship);

                var jsonOptions = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = null,
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                var schemaDataContent = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Version = updatedSchema.Version,
                    Tables = updatedSchema.Tables,
                    Relationships = updatedSchema.Relationships,
                    TermMappings = updatedSchema.TermMappings,
                    AnalysisResults = updatedSchema.AnalysisResults
                }, jsonOptions);

                // Send ONLY SchemaData
                var payload = new { SchemaData = schemaDataContent };

                var response = await Http.PutAsJsonAsync($"api/databaseschema/UpdateByDatabaseId/{DatabaseId}", payload);

                if (response.IsSuccessStatusCode)
                {
                    await LoadDatabaseSchema(false);
                    toastService.ShowSuccess("Suggested relationship added successfully.");
                    activeTab = "relationships";
                }
                else
                {
                    toastService.ShowError("Failed to add relationship.");
                }
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Error: {ex.Message + ", " + ex.StackTrace}");
            }
        }

        // Helper method - add if not present
        private string NormalizeRelationshipType(string type)
        {
            if (string.IsNullOrEmpty(type))
                return "One-to-Many";

            return type.ToLower().Replace(" ", "").Replace("_", "-") switch
            {
                "one-to-one" or "onetoone" or "1:1" => "One-to-One",
                "one-to-many" or "onetomany" or "1:n" or "1:*" => "One-to-Many",
                "many-to-one" or "manytoone" or "n:1" or "*:1" => "Many-to-One",
                "many-to-many" or "manytomany" or "n:n" or "*:*" => "Many-to-Many",
                _ => "One-to-Many"
            };
        }





        #region TermMapping

        private async Task SuggestTermMappings()
        {
            if (DatabaseId <= 0)
            {
                toastService.ShowWarning("Please select a database first.");
                return;
            }

            try
            {
                isSuggestingTerms = true;
                StateHasChanged();

                var suggestions = await Http.GetFromJsonAsync<List<TermMapping>>(
                    $"api/SchemaAnalysis/SuggestTerms/{DatabaseId}");

                if (suggestions?.Any() == true)
                {
                    // Initialize if null
                    if (schemaObj.TermMappings == null)
                        schemaObj.TermMappings = new List<TermMapping>();

                    // Filter out suggestions that already exist (by business term)
                    var existingTerms = schemaObj.TermMappings
                        .Select(t => t.BusinessTerm.ToLower())
                        .ToHashSet();

                    var newSuggestions = suggestions
                        .Where(s => !existingTerms.Contains(s.BusinessTerm.ToLower()))
                        .ToList();

                    if (newSuggestions.Any())
                    {
                        // Mark all as LLM suggested
                        foreach (var suggestion in newSuggestions)
                        {
                            suggestion.ID = Guid.NewGuid().ToString();
                            suggestion.IsLLMSuggested = true;
                            suggestion.CreatedAt = DateTime.UtcNow;
                        }

                        schemaObj.TermMappings.AddRange(newSuggestions);

                        // Save the updated term mappings
                        var saveResult = await SaveTermMappingsAsync();

                        if (saveResult)
                        {
                            toastService.ShowSuccess($"Generated {newSuggestions.Count} new term suggestions!");
                        }
                        else
                        {
                            toastService.ShowError("Failed to save term suggestions.");
                        }
                    }
                    else
                    {
                        toastService.ShowInfo("All suggested terms already exist in your mappings.");
                    }
                }
                else
                {
                    toastService.ShowWarning("No term suggestions could be generated. Try adding more table and column descriptions first.");
                }
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Failed to generate term suggestions: {ex.Message}");
            }
            finally
            {
                isSuggestingTerms = false;
                StateHasChanged();
            }
        }

        private async Task ShowTermSuggestionsDialog(List<TermMapping> suggestions)
        {
            // This would display a modal to review and select suggested terms
            // For simplicity, we'll just add all suggestions to the schema

            if (schemaObj.TermMappings == null)
                schemaObj.TermMappings = new List<TermMapping>();

            // Filter out suggestions that already exist (by business term)
            var existingTerms = schemaObj.TermMappings.Select(t => t.BusinessTerm.ToLower()).ToHashSet();
            var newSuggestions = suggestions.Where(s => !existingTerms.Contains(s.BusinessTerm.ToLower())).ToList();

            if (newSuggestions.Any())
            {
                schemaObj.TermMappings.AddRange(newSuggestions);

                // Save the updated schema
                if (!await SaveTermMappingsAsync())
                {
                    toastService.ShowError("Failed to save term mapping. Please try again.");
                }

                toastService.ShowSuccess($"Added {newSuggestions.Count} new term mappings!");
            }
            else
            {
                toastService.ShowInfo("All suggested terms are already mapped.");
            }
        }

        private void CreateNewTermMapping()
        {
            editingTermMapping = new TermMapping
            {
                ID = Guid.NewGuid().ToString(),
                Type = TermMappingType.DirectColumn,
                IsActive = true,
                IsLLMSuggested = false
            };

            isTermMappingModalOpen = true;
        }

        private void EditTermMapping(TermMapping mapping)
        {
            // Clone the mapping to avoid direct edits
            editingTermMapping = CloneTermMapping(mapping);
            isTermMappingModalOpen = true;
        }

        private TermMapping CloneTermMapping(TermMapping source)
        {
            // Deep clone to avoid modifying the original
            return new TermMapping
            {
                ID = source.ID,
                BusinessTerm = source.BusinessTerm,
                Description = source.Description,
                Type = source.Type,
                TableId = source.TableId,
                ColumnId = source.ColumnId,
                Formula = source.Formula,
                Dependencies = source.Dependencies?.Select(d => new TermMappingDependency
                {
                    TableId = d.TableId,
                    ColumnId = d.ColumnId,
                    TableName = d.TableName,
                    ColumnName = d.ColumnName
                }).ToList() ?? new List<TermMappingDependency>(),
                FilterCondition = source.FilterCondition,
                Synonyms = source.Synonyms?.ToList() ?? new List<string>(),
                IsActive = source.IsActive,
                IsLLMSuggested = source.IsLLMSuggested,
                CreatedAt = source.CreatedAt,
                ModifiedAt = DateTime.UtcNow
            };
        }

        private async Task SaveTermMapping()
        {
            if (editingTermMapping == null) return;

            // Validate required fields based on type
            if (string.IsNullOrWhiteSpace(editingTermMapping.BusinessTerm))
            {
                toastService.ShowError("Business term is required.");
                return;
            }

            // Validate based on mapping type
            switch (editingTermMapping.Type)
            {
                case TermMappingType.DirectColumn:
                    if (string.IsNullOrEmpty(editingTermMapping.TableId) || string.IsNullOrEmpty(editingTermMapping.ColumnId))
                    {
                        toastService.ShowError("Please select a table and column for direct column mapping.");
                        return;
                    }
                    break;

                case TermMappingType.CalculatedField:
                    if (string.IsNullOrWhiteSpace(editingTermMapping.Formula))
                    {
                        toastService.ShowError("Formula is required for calculated fields.");
                        return;
                    }
                    if (editingTermMapping.Dependencies == null || !editingTermMapping.Dependencies.Any())
                    {
                        toastService.ShowError("At least one column dependency is required for calculated fields.");
                        return;
                    }
                    break;

                case TermMappingType.FilterCondition:
                    if (string.IsNullOrWhiteSpace(editingTermMapping.FilterCondition))
                    {
                        toastService.ShowError("Filter condition is required.");
                        return;
                    }
                    break;
            }

            try
            {
                // Find existing or add new
                var existingIndex = schemaObj.TermMappings.FindIndex(t => t.ID == editingTermMapping.ID);

                if (existingIndex >= 0)
                {
                    schemaObj.TermMappings[existingIndex] = editingTermMapping;
                }
                else
                {
                    schemaObj.TermMappings.Add(editingTermMapping);
                }

                // Save to database
                if (!await SaveTermMappingsAsync())
                {
                    toastService.ShowError("Failed to save term mapping. Please try again.");
                }

                isTermMappingModalOpen = false;
                editingTermMapping = null;

                toastService.ShowSuccess("Term mapping saved successfully!");
            }
            catch (Exception ex)
            {

                toastService.ShowError("Failed to save term mapping. Please try again.");
            }
        }

        private async Task<bool> SaveTermMappingsAsync()
        {
            try
            {
                // Update the schema with the term mappings
                var result = await Http.PostAsJsonAsync($"api/databaseschema/{DatabaseId}/termMappings", schemaObj.TermMappings);
                return result.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        private async Task ToggleTermMappingActive(TermMapping mapping)
        {
            try
            {
                mapping.IsActive = !mapping.IsActive;
                mapping.ModifiedAt = DateTime.UtcNow;

                if (!await SaveTermMappingsAsync())
                {
                    toastService.ShowError("Failed to save term mapping. Please try again.");
                }

                toastService.ShowSuccess($"Term mapping {(mapping.IsActive ? "activated" : "deactivated")} successfully.");
            }
            catch (Exception ex)
            {

                toastService.ShowError("Failed to update term mapping. Please try again.");
            }
        }

        private async Task DeleteTermMapping(string id)
        {
            try
            {
                bool confirmed = await JSRuntime.InvokeAsync<bool>("confirm", "Are you sure you want to delete this term mapping?");

                if (confirmed)
                {
                    schemaObj.TermMappings.RemoveAll(t => t.ID == id);
                    if (!await SaveTermMappingsAsync())
                    {
                        toastService.ShowError("Failed to save term mapping. Please try again.");
                    }
                    toastService.ShowSuccess("Term mapping deleted successfully.");
                }
            }
            catch (Exception ex)
            {
                toastService.ShowError("Failed to delete term mapping. Please try again.");
            }
        }

        private string GetTermTypeBadgeClass(TermMappingType type)
        {
            return type switch
            {
                TermMappingType.DirectColumn => "bg-primary",
                TermMappingType.CalculatedField => "bg-success",
                TermMappingType.Aggregate => "bg-info",
                TermMappingType.FilterCondition => "bg-warning text-dark",
                _ => "bg-secondary"
            };
        }

        private string GetTermTypeDisplay(TermMappingType type)
        {
            return type switch
            {
                TermMappingType.DirectColumn => "Column",
                TermMappingType.CalculatedField => "Calculated",
                TermMappingType.Aggregate => "Aggregate",
                TermMappingType.FilterCondition => "Filter",
                _ => type.ToString()
            };
        }



        private void AddSynonyms()
        {
            if (editingTermMapping == null || string.IsNullOrWhiteSpace(synonymInput))
                return;

            if (editingTermMapping.Synonyms == null)
                editingTermMapping.Synonyms = new List<string>();

            var newSynonyms = synonymInput.Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            foreach (var synonym in newSynonyms)
            {
                if (!editingTermMapping.Synonyms.Contains(synonym, StringComparer.OrdinalIgnoreCase))
                {
                    editingTermMapping.Synonyms.Add(synonym);
                }
            }

            synonymInput = string.Empty;
        }

        private void RemoveSynonym(string synonym)
        {
            if (editingTermMapping?.Synonyms == null)
                return;

            editingTermMapping.Synonyms.Remove(synonym);
        }

        private void AddDependency()
        {
            editingDependency = new TermMappingDependency();
            isDependencyModalOpen = true;
        }

        private void RemoveDependency(TermMappingDependency dependency)
        {
            if (editingTermMapping?.Dependencies == null)
                return;

            editingTermMapping.Dependencies.Remove(dependency);
        }



        private async Task SaveAllTermSuggestions()
        {
            try
            {
                isSavingAllSuggestions = true;

                // Find all unconfirmed LLM-suggested terms
                //var suggestedTerms = schemaObj.TermMappings
                //    .Where(t => t.IsLLMSuggested && !t.IsConfirmed)
                //    .ToList();

                //if (!suggestedTerms.Any())
                //{
                //    toastService.ShowInfo("No unconfirmed suggestions to save.");
                //    return;
                //}

                //// Mark all as confirmed
                ////TODO : TO complete this or ignore it
                //foreach (var term in suggestedTerms)
                //{
                //    term.IsConfirmed = true;
                //}

                // Save to database
                if (!await SaveTermMappingsAsync())
                {
                    toastService.ShowError("Failed to save term mapping. Please try again.");
                }

                toastService.ShowSuccess($"Successfully accepted & saved {schemaObj.TermMappings.Count} Terms and Formulas");
            }
            catch (Exception ex)
            {
                toastService.ShowError("Failed to save Terms and Formulas. Please try again.");
            }
            finally
            {
                isSavingAllSuggestions = false;
            }
        }

        private async Task AcceptTermSuggestion(TermMapping term)
        {
            try
            {
                term.IsConfirmed = true;
                if (!await SaveTermMappingsAsync())
                {
                    toastService.ShowError("Failed to save term mapping. Please try again.");
                }
                toastService.ShowSuccess($"Accepted term: {term.BusinessTerm}");
            }
            catch (Exception ex)
            {
                toastService.ShowError("Failed to accept term suggestion. Please try again.");
            }
        }

        private async Task IgnoreTermSuggestion(TermMapping term)
        {
            try
            {
                // Find index of term
                var index = schemaObj.TermMappings.FindIndex(t => t.ID == term.ID);
                if (index >= 0)
                {
                    // Remove the term
                    schemaObj.TermMappings.RemoveAt(index);
                    if (!await SaveTermMappingsAsync())
                    {
                        toastService.ShowError("Failed to save term mapping. Please try again.");
                    }
                    toastService.ShowSuccess($"Ignored term: {term.BusinessTerm}");
                }
            }
            catch (Exception ex)
            {
                toastService.ShowError("Failed to ignore term suggestion. Please try again.");
            }
        }

        private void ShowFormulaViewer(TermMapping term)
        {
            currentFormulaTerm = term;
            formulaText = term.Formula;
            isEditingFormula = false;
            isFormulaModalOpen = true;
            formulaValidationResult = null;
        }

        private void EnableFormulaEditing()
        {
            isEditingFormula = true;
            ValidateFormula(formulaText);
        }

        private void CancelFormulaEdit()
        {
            isEditingFormula = false;
            formulaText = currentFormulaTerm.Formula;
            formulaValidationResult = null;
        }

        private async Task SaveFormulaChanges()
        {
            if (currentFormulaTerm == null) return;

            try
            {
                // Validate formula one more time
                await ValidateFormula(formulaText);

                if (formulaValidationResult == null || !formulaValidationResult.IsValid)
                {
                    toastService.ShowWarning("Cannot save invalid formula. Please correct errors first.");
                    return;
                }

                // Update the formula
                currentFormulaTerm.Formula = formulaText;

                // Update dependencies based on validation result
                if (formulaValidationResult.ReferencedObjects?.TableColumns != null)
                {
                    // Clear existing dependencies
                    currentFormulaTerm.Dependencies.Clear();

                    // Add new dependencies based on references
                    foreach (var tableCol in formulaValidationResult.ReferencedObjects.TableColumns)
                    {
                        string tableName = tableCol.Key;
                        var tableObj = schemaObj.Tables.FirstOrDefault(t =>
                            t.DBName.Equals(tableName, StringComparison.OrdinalIgnoreCase));

                        if (tableObj != null)
                        {
                            foreach (string colName in tableCol.Value)
                            {
                                var colObj = tableObj.Columns.FirstOrDefault(c =>
                                    c.DBName.Equals(colName, StringComparison.OrdinalIgnoreCase));

                                if (colObj != null)
                                {
                                    currentFormulaTerm.Dependencies.Add(new TermMappingDependency
                                    {
                                        TableId = tableObj.ID,
                                        ColumnId = colObj.ID,
                                        TableName = tableObj.DBName,
                                        ColumnName = colObj.DBName
                                    });
                                }
                            }
                        }
                    }
                }

                // Save changes
                if (!await SaveTermMappingsAsync())
                {
                    toastService.ShowError("Failed to save term mapping. Please try again.");
                }

                // Close modal
                isFormulaModalOpen = false;
                isEditingFormula = false;
                currentFormulaTerm = null;
                formulaText = null;
                formulaValidationResult = null;

                toastService.ShowSuccess("Formula saved successfully!");
            }
            catch (Exception ex)
            {
                toastService.ShowError("Failed to save formula. Please try again.");
            }
        }

        private void CloseFormulaModal()
        {
            isFormulaModalOpen = false;
            isEditingFormula = false;
            currentFormulaTerm = null;
            formulaText = null;
            formulaValidationResult = null;
        }

        private async Task ValidateFormula(string formula)
        {
            if (string.IsNullOrWhiteSpace(formula))
            {
                formulaValidationResult = new QueryValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Formula cannot be empty"
                };
                return;
            }

            try
            {
                isValidatingFormula = true;

                // Create a validation request for the formula
                // We need to wrap it in a basic SELECT to validate
                string testQuery = $"SELECT {formula} AS Result FROM (SELECT 1 AS DummyValue) AS Dummy";

                // Call validation endpoint
                var response = await Http.PostAsJsonAsync(
                    $"api/query/validate-formula?databaseId={DatabaseId}",
                    new { Formula = formula });

                if (response.IsSuccessStatusCode)
                {
                    formulaValidationResult = await response.Content.ReadFromJsonAsync<QueryValidationResult>();
                }
                else
                {
                    formulaValidationResult = new QueryValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Failed to validate formula. Server error occurred."
                    };
                }
            }
            catch (Exception ex)
            {
                formulaValidationResult = new QueryValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Validation error: {ex.Message + ", " + ex.StackTrace}"
                };
            }
            finally
            {
                isValidatingFormula = false;
            }
        }


        #endregion

        // Helper methods
        private TableDescription tableAnalysis => GetTableAnalysis();

        private TableDescription GetTableAnalysis()
        {
            if (selectedTable == null || analysisResult?.AnalysisData?.TableDescriptions == null)
                return null;

            return analysisResult.AnalysisData.TableDescriptions
                .FirstOrDefault(t => t.TableName.Equals(selectedTable.DBName, StringComparison.OrdinalIgnoreCase));
        }

        private ColumnDescription GetColumnAnalysis(ColumnSchema column)
        {
            if (column == null || selectedTable == null || analysisResult?.AnalysisData?.ColumnDescriptions == null)
                return null;

            return analysisResult.AnalysisData.ColumnDescriptions
                .FirstOrDefault(c =>
                    c.TableName.Equals(selectedTable.DBName, StringComparison.OrdinalIgnoreCase) &&
                    c.ColumnName.Equals(column.DBName, StringComparison.OrdinalIgnoreCase));
        }

        private string GetTableName(string tableId)
        {
            if (string.IsNullOrEmpty(tableId))
                return "Unknown";

            var table = schemaObj?.Tables?.FirstOrDefault(t => t.ID == tableId);
            return table != null ?
                (!string.IsNullOrEmpty(table.FriendlyName) ? table.FriendlyName : table.DBName) :
                $"Table {tableId}";
        }

        private string GetColumnName(string tableId, string columnId)
        {
            if (string.IsNullOrEmpty(tableId) || string.IsNullOrEmpty(columnId))
                return "Unknown";

            var table = schemaObj?.Tables?.FirstOrDefault(t => t.ID == tableId);
            if (table?.Columns == null)
                return $"Column {columnId}";

            var column = table.Columns.FirstOrDefault(c => c.ID == columnId);
            return column != null ?
                (!string.IsNullOrEmpty(column.FriendlyName) ? column.FriendlyName : column.DBName) :
                $"Column {columnId}";
        }

        private DatabaseSchema CloneSchema(DatabaseSchema source)
        {
            if (source == null)
                return null;

            // Using JSON serialization for deep cloning
            var json = System.Text.Json.JsonSerializer.Serialize(source);
            return System.Text.Json.JsonSerializer.Deserialize<DatabaseSchema>(json);
        }

        // Filtered collections
        private IEnumerable<TableSchema> FilteredTables =>
            schemaObj?.Tables?.Where(t =>
                string.IsNullOrEmpty(tableSearchTerm) ||
                t.DBName.Contains(tableSearchTerm, StringComparison.OrdinalIgnoreCase) ||
                (t.FriendlyName?.Contains(tableSearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.Description?.Contains(tableSearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
            ).OrderBy(t => t.DBName) ?? Enumerable.Empty<TableSchema>();

        private IEnumerable<ColumnSchema> FilteredColumns
        {
            get
            {
                if (selectedTable?.Columns == null)
                    return Enumerable.Empty<ColumnSchema>();

                var filtered = selectedTable.Columns.AsEnumerable();

                // Apply search term filter
                if (!string.IsNullOrEmpty(columnSearchTerm))
                {
                    filtered = filtered.Where(c =>
                        c.DBName.Contains(columnSearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        (c.FriendlyName?.Contains(columnSearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (c.Description?.Contains(columnSearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
                    );
                }

                // Apply type filter
                switch (columnFilter)
                {
                    case "primary":
                        filtered = filtered.Where(c => c.IsPrimaryKey);
                        break;
                    case "lookup":
                        filtered = filtered.Where(c => c.IsLookup);
                        break;
                    case "active":
                        filtered = filtered.Where(c => c.IsActive);
                        break;
                    case "inactive":
                        filtered = filtered.Where(c => !c.IsActive);
                        break;
                }

                return filtered.OrderBy(c => c.DBName);
            }
        }

        private IEnumerable<RelationshipSchema> FilteredRelationships
        {
            get
            {
                if (relationships == null)
                    return Enumerable.Empty<RelationshipSchema>();

                var filtered = relationships.AsEnumerable();

                // Apply search term filter
                if (!string.IsNullOrEmpty(relationshipSearchTerm))
                {
                    filtered = filtered.Where(r =>
                        (r.Name?.Contains(relationshipSearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (r.Source?.TableName?.Contains(relationshipSearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (r.Source?.ColumnName?.Contains(relationshipSearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (r.Target?.TableName?.Contains(relationshipSearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (r.Target?.ColumnName?.Contains(relationshipSearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
                    );
                }

                // Apply type filter
                switch (relationshipFilter.ToLowerInvariant())
                {
                    case "onetomany":
                        filtered = filtered.Where(r => r.Type == "One-to-Many");
                        break;
                    case "manytoone":
                        filtered = filtered.Where(r => r.Type == "Many-to-One");
                        break;
                    case "onetoone":
                        filtered = filtered.Where(r => r.Type == "One-to-One");
                        break;
                    case "manytomany":
                        filtered = filtered.Where(r => r.Type == "Many-to-Many");
                        break;
                    case "active":
                        filtered = filtered.Where(r => r.IsActive);
                        break;
                    case "inactive":
                        filtered = filtered.Where(r => !r.IsActive);
                        break;
                }

                return filtered;
            }
        }

        private async Task GetOpimizedDBSchema()
        {
            schemaObj = await Http.GetFromJsonAsync<DatabaseSchema>($"api/databaseschema/OptimizedSchemaString/{DatabaseId}");

            toastService.ShowSuccess("Optimized Schema Loaded Successfully.");
        }

        /// <summary>
        /// Clears all cache - for testing purposes
        /// </summary>
        private async Task ClearCacheAsync()
        {
            try
            {
                await Http.PostAsync("api/databaseschema/cache/clearall", null);
                Console.WriteLine("Cache cleared on page load");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to clear cache: {ex.Message + ", " + ex.StackTrace}");
                // Don't throw - cache clear failure shouldn't block page load
            }
        }


    }


}
