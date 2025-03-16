using Microsoft.AspNetCore.Components;
using DynamicDashboardCommon.Models;

using System.Threading.Tasks;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Reflection;

namespace DynamicDashboardFE.Pages.Admin
{
    public partial class DBMetaDataV2_1 : ComponentBase
    {


        [Parameter]
        public int DatabaseId { get; set; }



        // State variables
        private string activeTab = "tables";
        private string analysisTab = "tables";
        private bool isLoading = true;
        private bool isAnalyzing = false;
        private bool isApplying = false;
        private string loadingMessage = "Loading database metadata...";

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


        // Term Mapping
        private bool isSuggestingTerms = false;
        private TermMapping editingTermMapping;
        private bool isTermMappingModalOpen = false;
        private string termSearchTerm = string.Empty;
        private string termTypeFilter = "all";
        private string synonymInput = string.Empty;
        private TermMappingDependency editingDependency;
        private bool isDependencyModalOpen = false;

        protected override async Task OnInitializedAsync()
        {
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
                toastService.ShowError($"Error loading databases: {ex.Message}");
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
                toastService.ShowError($"Error loading database: {ex.Message}");
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task LoadDatabaseSchema(bool useCache = true)
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
                toastService.ShowError($"Error loading schema: {ex.Message}");
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
                toastService.ShowError($"Error refreshing schema: {ex.Message}");
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
                toastService.ShowError($"Error saving table changes: {ex.Message}");
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
                toastService.ShowError($"Error updating table status: {ex.Message}");
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
                toastService.ShowError($"Error saving column changes: {ex.Message}");
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
                toastService.ShowError($"Error updating column status: {ex.Message}");
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

            try
            {
                isSavingRelationship = true;

                // Clone the schema before modifying
                var updatedSchema = CloneSchema(schemaObj);

                // Find or create relationship in the schema
                int relationshipIndex = updatedSchema.Relationships.FindIndex(r => r.ID == editingRelationship.ID);

                if (relationshipIndex >= 0)
                {
                    // Update existing relationship
                    updatedSchema.Relationships[relationshipIndex] = editingRelationship;
                }
                else
                {
                    // Add new relationship
                    updatedSchema.Relationships.Add(editingRelationship);
                }

                // Set table and column names based on IDs
                UpdateRelationshipNames(updatedSchema, editingRelationship);

                // Save the updated schema
                await Http.PutAsync($"api/databaseschema/{DatabaseId}", JsonContent.Create(updatedSchema));

                // Reload the schema to reflect changes
                await LoadDatabaseSchema();

                toastService.ShowSuccess($"Relationship {(isNewRelationship ? "created" : "updated")} successfully.");
                CloseRelationshipEditor();
            }
            catch (Exception ex)
            {
                relationshipErrorMessage = $"Error saving relationship: {ex.Message}";
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
                // Clone the schema before modifying
                var updatedSchema = CloneSchema(schemaObj);

                // Remove the relationship
                updatedSchema.Relationships.RemoveAll(r => r.ID == relationshipId);

                // Save the updated schema
                await Http.PutAsync($"api/databaseschema/{DatabaseId}", JsonContent.Create(updatedSchema));

                // Reload the schema to reflect changes
                await LoadDatabaseSchema();

                toastService.ShowSuccess("Relationship deleted successfully.");
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Error deleting relationship: {ex.Message}");
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
                toastService.ShowError($"Error updating relationship status: {ex.Message}");
            }
        }

        // Schema analysis
        private async Task RunSchemaAnalysis()
        {
            isAnalyzing = true;

            try
            {
                analysisResult = await Http.GetFromJsonAsync<SchemaAnalysisResult>($"api/SchemaAnalysis/analyze/{DatabaseId}");

                if (analysisResult?.Success == true)
                {
                    toastService.ShowSuccess("Schema analysis completed successfully.");

                    // Set active tab to analysis results
                    activeTab = "analysis";
                }
                else
                {
                    toastService.ShowError($"Schema analysis failed: {analysisResult?.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Error analyzing schema: {ex.Message}");
            }
            finally
            {
                isAnalyzing = false;
            }
        }

        private async Task ApplyAllSuggestions()
        {
            if (analysisResult?.AnalysisData == null)
                return;

            isApplying = true;

            try
            {
                var result = await Http.PostAsJsonAsync($"api/SchemaAnalysis/apply/{DatabaseId}", analysisResult.AnalysisData);

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
                toastService.ShowError($"Error applying suggestions: {ex.Message}");
            }
            finally
            {
                isApplying = false;
            }
        }

        // Add to DBMetaDataV2.razor.cs

        /// <summary>
        /// Analyzes only the tables in the database schema
        /// </summary>
        private async Task AnalyzeTablesOnly()
        {
            if (DatabaseId <= 0)
                return;

            isAnalyzing = true;
            analysisTab = "tables"; // Auto-select tables tab for results

            try
            {
                var optimizedSchema = await GetOptimizedSchemaForAnalysis();

                // Create a focused prompt for table analysis only
                var analysisRequest = new SchemaAnalysisRequest
                {
                    DatabaseId = DatabaseId,
                    SchemaString = optimizedSchema,
                    AnalysisMode = "tables-only" // Signal to the backend what to analyze
                };

                var result = await Http.PostAsJsonAsync(
                    "api/SchemaAnalysis/analyze-tables", analysisRequest);

                analysisResult = await result.Content.ReadFromJsonAsync<SchemaAnalysisResult>();

                if (analysisResult?.Success == true)
                {
                    toastService.ShowSuccess("Table analysis completed successfully.");
                    activeTab = "analysis"; // Switch to analysis tab to show results
                }
                else
                {
                    toastService.ShowError($"Table analysis failed:");
                }
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Error analyzing tables: {ex.Message}");
            }
            finally
            {
                isAnalyzing = false;
            }
        }

        /// <summary>
        /// Analyzes only the columns in the database schema
        /// </summary>
        private async Task AnalyzeColumnsOnly()
        {
            if (DatabaseId <= 0)
                return;

            isAnalyzing = true;
            analysisTab = "columns"; // Auto-select columns tab for results

            try
            {
                var optimizedSchema = await GetOptimizedSchemaForAnalysis();

                var analysisRequest = new SchemaAnalysisRequest
                {
                    DatabaseId = DatabaseId,
                    SchemaString = optimizedSchema,
                    AnalysisMode = "columns-only"
                };

                var result = await Http.PostAsJsonAsync(
                    "api/SchemaAnalysis/analyze-columns", analysisRequest);

                analysisResult = await result.Content.ReadFromJsonAsync<SchemaAnalysisResult>();

                if (analysisResult?.Success == true)
                {
                    toastService.ShowSuccess("Column analysis completed successfully.");
                    activeTab = "analysis";
                }
                else
                {
                    toastService.ShowError($"Column analysis failed: {analysisResult?.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Error analyzing columns: {ex.Message}");
            }
            finally
            {
                isAnalyzing = false;
            }
        }

        /// <summary>
        /// Analyzes only the relationships in the database schema
        /// </summary>
        private async Task AnalyzeRelationshipsOnly()
        {
            if (DatabaseId <= 0)
                return;

            isAnalyzing = true;
            analysisTab = "relationships"; // Auto-select relationships tab for results

            try
            {
                var optimizedSchema = await GetOptimizedSchemaForAnalysis();

                var analysisRequest = new SchemaAnalysisRequest
                {
                    DatabaseId = DatabaseId,
                    SchemaString = optimizedSchema,
                    AnalysisMode = "relationships-only"
                };

                var result = await Http.PostAsJsonAsync(
                    "api/SchemaAnalysis/analyze-relationships", analysisRequest);

                analysisResult = await result.Content.ReadFromJsonAsync<SchemaAnalysisResult>();

                if (analysisResult?.Success == true)
                {
                    toastService.ShowSuccess("Relationship analysis completed successfully.");
                    activeTab = "analysis";
                }
                else
                {
                    toastService.ShowError($"Relationship analysis failed: {analysisResult?.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Error analyzing relationships: {ex.Message}");
            }
            finally
            {
                isAnalyzing = false;
            }
        }

        /// <summary>
        /// Analyzes potential conflicts in the database schema
        /// </summary>
        private async Task AnalyzeConflictsOnly()
        {
            if (DatabaseId <= 0)
                return;

            isAnalyzing = true;
            analysisTab = "conflicts"; // Auto-select conflicts tab for results

            try
            {
                var optimizedSchema = await GetOptimizedSchemaForAnalysis();

                var analysisRequest = new SchemaAnalysisRequest
                {
                    DatabaseId = DatabaseId,
                    SchemaString = optimizedSchema,
                    AnalysisMode = "conflicts-only"
                };

                var result = await Http.PostAsJsonAsync(
                    "api/SchemaAnalysis/analyze-conflicts", analysisRequest);

                analysisResult = await result.Content.ReadFromJsonAsync<SchemaAnalysisResult>();

                if (analysisResult?.Success == true)
                {
                    toastService.ShowSuccess("Conflict analysis completed successfully.");
                    activeTab = "analysis";
                }
                else
                {
                    toastService.ShowError($"Conflict analysis failed: {analysisResult?.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Error analyzing conflicts: {ex.Message}");
            }
            finally
            {
                isAnalyzing = false;
            }
        }

        /// <summary>
        /// Helper method to get optimized schema for analysis
        /// </summary>
        private async Task<string> GetOptimizedSchemaForAnalysis()
        {
            try
            {
                // Get the optimized schema string directly from the API
                var response = await Http.GetStringAsync($"api/databaseschema/OptimizedSchemaString/{DatabaseId}");
                return response;
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Error getting schema: {ex.Message}");
                throw;
            }
        }

        // Suggestions and conflicts
        private async Task ApplyTableSuggestion(TableDescription suggestion = null)
        {
            if (suggestion == null)
            {
                // Use the current table suggestion
                suggestion = tableAnalysis;
            }

            if (suggestion == null)
                return;

            try
            {
                // Find the table in the schema
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

                toastService.ShowSuccess("Table suggestion applied. Don't forget to save your changes.");
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Error applying table suggestion: {ex.Message}");
            }
        }

        private async Task ApplyColumnSuggestion(ColumnDescription suggestion)
        {
            if (suggestion == null)
                return;

            try
            {
                // Find the table and column in the schema
                var table = schemaObj.Tables.FirstOrDefault(t =>
                    t.DBName.Equals(suggestion.TableName, StringComparison.OrdinalIgnoreCase));

                if (table?.Columns == null)
                    return;

                var column = table.Columns.FirstOrDefault(c =>
                    c.DBName.Equals(suggestion.ColumnName, StringComparison.OrdinalIgnoreCase));

                if (column == null)
                    return;

                // Update the column with suggested values
                column.FriendlyName = suggestion.SuggestedName;
                column.Description = suggestion.SuggestedDescription;
                column.IsLookup = suggestion.IsLookupColumn;

                toastService.ShowSuccess("Column suggestion applied. Don't forget to save your changes.");
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Error applying column suggestion: {ex.Message}");
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
                toastService.ShowError($"Error applying conflict resolution: {ex.Message}");
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
                toastService.ShowError($"Error applying suggestion: {ex.Message}");
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
                    t.DBName.Equals(suggestion.SourceTable.TableName, StringComparison.OrdinalIgnoreCase));

                if (sourceTable?.Columns == null)
                    return;

                var sourceColumn = sourceTable.Columns.FirstOrDefault(c =>
                    c.DBName.Equals(suggestion.SourceTable.ColumnName, StringComparison.OrdinalIgnoreCase));

                if (sourceColumn == null)
                    return;

                // Find target table and column
                var targetTable = schemaObj.Tables.FirstOrDefault(t =>
                    t.DBName.Equals(suggestion.TargetTable.TableName, StringComparison.OrdinalIgnoreCase));

                if (targetTable?.Columns == null)
                    return;

                var targetColumn = targetTable.Columns.FirstOrDefault(c =>
                    c.DBName.Equals(suggestion.TargetTable.ColumnName, StringComparison.OrdinalIgnoreCase));

                if (targetColumn == null)
                    return;

                // Create a new relationship
                var newRelationship = new RelationshipSchema
                {
                    ID = Guid.NewGuid().ToString(),
                    Name = $"Relationship from {sourceTable.DBName}.{sourceColumn.DBName} to {targetTable.DBName}.{targetColumn.DBName}",
                    Type = suggestion.RelationshipType,
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
                    IsActive = true
                };

                // Add the new relationship to the schema
                var updatedSchema = CloneSchema(schemaObj);
                updatedSchema.Relationships.Add(newRelationship);

                // Save the updated schema
                await Http.PutAsync($"api/databaseschema/{DatabaseId}", JsonContent.Create(updatedSchema));

                // Reload the schema to reflect changes
                await LoadDatabaseSchema();

                toastService.ShowSuccess("Suggested relationship added successfully.");

                // Switch to relationships tab
                activeTab = "relationships";
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Error adding relationship: {ex.Message}");
            }
        }

        // Term mapping
        private async Task SuggestTermMappings()
        {
            try
            {
                isSuggestingTerms = true;

                var suggestions = await Http.GetFromJsonAsync<List<TermMapping>>($"api/databaseschema/suggestTerms/{DatabaseId}");

                if (suggestions?.Any() == true)
                {
                    // Show suggestions dialog
                    await ShowTermSuggestionsDialog(suggestions);
                }
                else
                {
                    toastService.ShowWarning("No term suggestions could be generated. Try adding more table and column descriptions.");
                }
            }
            catch (Exception ex)
            {

                toastService.ShowError("Failed to generate term suggestions. Please try again.");
            }
            finally
            {
                isSuggestingTerms = false;
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
                await SaveTermMappingsAsync();

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
                await SaveTermMappingsAsync();

                isTermMappingModalOpen = false;
                editingTermMapping = null;

                toastService.ShowSuccess("Term mapping saved successfully!");
            }
            catch (Exception ex)
            {

                toastService.ShowError("Failed to save term mapping. Please try again.");
            }
        }

        private async Task SaveTermMappingsAsync()
        {
            try
            {
                // Update the schema with the term mappings
                await Http.PostAsJsonAsync($"api/databaseschema/{DatabaseId}/termMappings", schemaObj.TermMappings);
            }
            catch (Exception ex)
            {

                toastService.ShowError("Failed to save term mappings. Please try again.");
            }
        }

        private async Task ToggleTermMappingActive(TermMapping mapping)
        {
            try
            {
                mapping.IsActive = !mapping.IsActive;
                mapping.ModifiedAt = DateTime.UtcNow;

                await SaveTermMappingsAsync();

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
                    await SaveTermMappingsAsync();
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

        // Add to DBMetaDataV2.razor.cs

        /// <summary>
        /// Analyzes and generates term mappings for the database schema
        /// </summary>
        private async Task AnalyzeTermMappings()
        {
            if (DatabaseId <= 0)
                return;

            isAnalyzing = true;

            try
            {
                var optimizedSchema = await GetOptimizedSchemaForAnalysis();

                var analysisRequest = new SchemaAnalysisRequest
                {
                    DatabaseId = DatabaseId,
                    SchemaString = optimizedSchema,
                    AnalysisMode = "term-mappings"
                };

                var result = await Http.PostAsJsonAsync(
                    "api/SchemaAnalysis/analyze-term-mappings", analysisRequest);

                List<TermMapping> lstTerms = await result.Content.ReadFromJsonAsync<List<TermMapping>>();

                if (lstTerms != null)
                {
                    // If schema object doesn't have TermMappings property, add it
                    if (schemaObj.TermMappings == null)
                    {
                        schemaObj.TermMappings = new List<TermMapping>();
                    }
                    else
                    {
                        schemaObj.TermMappings = lstTerms;
                    }

                    // Merge new mappings with existing ones


                    toastService.ShowSuccess("Term mappings generated successfully.");
                }
                else
                {
                    toastService.ShowError("Failed to generate term mappings.");
                }
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Error analyzing term mappings: {ex.Message}");
            }
            finally
            {
                isAnalyzing = false;
            }
        }

        /// <summary>
        /// Removes a term mapping
        /// </summary>
        private void RemoveTermMapping(string termID)
        {
            TermMapping objTermMapping = schemaObj.TermMappings.Find(t => t.ID == termID);
            if (objTermMapping != null)
            {
                schemaObj.TermMappings.Remove(objTermMapping);
            }

        }

        /// <summary>
        /// Saves term mappings to the database
        /// </summary>
        private async Task SaveTermMappings()
        {
            if (DatabaseId <= 0 || schemaObj?.TermMappings == null)
                return;

            try
            {
                var result = await Http.PostAsJsonAsync($"api/databaseschema/{DatabaseId}/term-mappings", schemaObj.TermMappings);

                if (result.IsSuccessStatusCode)
                {
                    toastService.ShowSuccess("Term mappings saved successfully.");
                }
                else
                {
                    toastService.ShowError("Failed to save term mappings.");
                }
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Error saving term mappings: {ex.Message}");
            }
        }

        private async Task UpdateColumnOptions()
        {
            // This is called when the table selection changes
            StateHasChanged();
        }

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



    }
}