using Microsoft.AspNetCore.Components;
using DynamicDashboardCommon.Models;
using DynamicDashboardFE.Utilities;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using System.Net.Http.Json;

namespace DynamicDashboardFE.Pages.Admin
{
    public partial class DBMetaDataV2 : ComponentBase
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
                Notifications.ShowError($"Error loading databases: {ex.Message}");
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
                Notifications.ShowError($"Error loading database: {ex.Message}");
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task LoadDatabaseSchema()
        {
            try
            {
                // Get parsed schema
                schemaObj = await Http.GetFromJsonAsync<DatabaseSchema>($"api/databaseschema/parsed/{DatabaseId}");

                // Load relationships
                relationships = schemaObj?.Relationships ?? new List<RelationshipSchema>();
            }
            catch (Exception ex)
            {
                Notifications.ShowError($"Error loading schema: {ex.Message}");
            }
        }

        private async Task RefreshSchema()
        {
            isLoading = true;
            loadingMessage = "Refreshing database schema...";

            try
            {
                await Http.PostAsync($"api/databaseschema/refresh/{DatabaseId}", null);
                await LoadDatabaseSchema();
                Notifications.ShowSuccess("Schema refreshed successfully.");
            }
            catch (Exception ex)
            {
                Notifications.ShowError($"Error refreshing schema: {ex.Message}");
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
                    Synonyms = selectedTable.Synonyms
                };

                // Use a dedicated endpoint for updating just the table
                await Http.PutAsync($"api/databaseschema/table/update/{DatabaseId}/{selectedTable.ID}",
                                   JsonContent.Create(tableUpdate));

                // No need to reload the entire schema - the UI already has the updated data
                Notifications.ShowSuccess("Table changes saved successfully.");
            }
            catch (Exception ex)
            {
                Notifications.ShowError($"Error saving table changes: {ex.Message}");
            }
        }

        private async Task UpdateTableActiveStatus(object value)
        {
            if (selectedTable == null || !(value is bool))
                return;

            bool isActive = (bool)value;
            try
            {
                await Http.PutAsync($"api/databaseschema/tables/{DatabaseId}/{selectedTable.ID}/active",
                                    JsonContent.Create(isActive));

                selectedTable.IsActive = isActive;
                Notifications.ShowSuccess($"Table {(isActive ? "activated" : "deactivated")} successfully.");
            }
            catch (Exception ex)
            {
                Notifications.ShowError($"Error updating table status: {ex.Message}");
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
                    Synonyms = c.Synonyms
                }).ToList();

                // Send only the column updates in a batch
                await Http.PutAsync($"api/databaseschema/columns/{DatabaseId}/{selectedTable.ID}",
                                   JsonContent.Create(columnUpdates));

                Notifications.ShowSuccess("Column changes saved successfully.");
            }
            catch (Exception ex)
            {
                Notifications.ShowError($"Error saving column changes: {ex.Message}");
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
                Notifications.ShowSuccess($"Column {(newStatus ? "activated" : "deactivated")} successfully.");
            }
            catch (Exception ex)
            {
                Notifications.ShowError($"Error updating column status: {ex.Message}");
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

                Notifications.ShowSuccess($"Relationship {(isNewRelationship ? "created" : "updated")} successfully.");
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

                Notifications.ShowSuccess("Relationship deleted successfully.");
            }
            catch (Exception ex)
            {
                Notifications.ShowError($"Error deleting relationship: {ex.Message}");
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
                Notifications.ShowSuccess($"Relationship {(newStatus ? "activated" : "deactivated")} successfully.");
            }
            catch (Exception ex)
            {
                Notifications.ShowError($"Error updating relationship status: {ex.Message}");
            }
        }

        // Schema analysis
        private async Task RunSchemaAnalysis()
        {
            isAnalyzing = true;

            try
            {
                analysisResult = await Http.GetFromJsonAsync<SchemaAnalysisResult>($"api/schema-analysis/analyze/{DatabaseId}");

                if (analysisResult?.Success == true)
                {
                    Notifications.ShowSuccess("Schema analysis completed successfully.");

                    // Set active tab to analysis results
                    activeTab = "analysis";
                }
                else
                {
                    Notifications.ShowError($"Schema analysis failed: {analysisResult?.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Notifications.ShowError($"Error analyzing schema: {ex.Message}");
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
                var result = await Http.PostAsJsonAsync($"api/schema-analysis/apply/{DatabaseId}", analysisResult.AnalysisData);

                if (result.IsSuccessStatusCode)
                {
                    await LoadDatabaseSchema();
                    Notifications.ShowSuccess("All suggestions applied successfully.");
                }
                else
                {
                    Notifications.ShowError("Failed to apply suggestions.");
                }
            }
            catch (Exception ex)
            {
                Notifications.ShowError($"Error applying suggestions: {ex.Message}");
            }
            finally
            {
                isApplying = false;
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

                Notifications.ShowSuccess("Table suggestion applied. Don't forget to save your changes.");
            }
            catch (Exception ex)
            {
                Notifications.ShowError($"Error applying table suggestion: {ex.Message}");
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

                Notifications.ShowSuccess("Column suggestion applied. Don't forget to save your changes.");
            }
            catch (Exception ex)
            {
                Notifications.ShowError($"Error applying column suggestion: {ex.Message}");
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
                        Notifications.ShowSuccess("Table conflict resolution applied. Don't forget to save your changes.");
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
                            Notifications.ShowSuccess("Column conflict resolution applied. Don't forget to save your changes.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Notifications.ShowError($"Error applying conflict resolution: {ex.Message}");
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
                        Notifications.ShowSuccess("Table description updated. Don't forget to save your changes.");
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
                            Notifications.ShowSuccess("Column description updated. Don't forget to save your changes.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Notifications.ShowError($"Error applying suggestion: {ex.Message}");
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

                Notifications.ShowSuccess("Suggested relationship added successfully.");

                // Switch to relationships tab
                activeTab = "relationships";
            }
            catch (Exception ex)
            {
                Notifications.ShowError($"Error adding relationship: {ex.Message}");
            }
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
    
}
}