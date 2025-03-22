using DynamicDashboardCommon.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Net.Http.Json;

namespace DynamicDashboardFE.Pages.Admin
{
    public partial class DatabaseMetadataManager : ComponentBase
    {
        [Parameter]
        public int DatabaseId { get; set; }

        private string databaseName;
        private bool isLoading = true;
        private bool isAnalyzing = false;
        private bool isApplying = false;
        private string loadingMessage = "Loading database metadata...";
        private string currentView = "tables";

        private List<Table> tables = new List<Table>();
        private List<Column> columns = new List<Column>();
        private List<Relationship> relationships = new List<Relationship>();

        private Table selectedTable;
        private Column selectedColumn;
        private ColumnDescription selectedColumnAnalysis;
        private bool isColumnSuggestionModalOpen = false;

        private SchemaAnalysisResult analysisResult;
        private bool hasAnalysisResults => analysisResult?.AnalysisData != null;
        private bool hasConflicts => analysisResult?.AnalysisData?.PotentialConflicts?.Count > 0;
        private bool hasUnclearElements => analysisResult?.AnalysisData?.UnclearElements?.Count > 0;
        private bool hasSuggestedRelationships => analysisResult?.AnalysisData?.SuggestedRelationships?.Count > 0;

        private int conflictCount => analysisResult?.AnalysisData?.PotentialConflicts?.Count ?? 0;
        private int unclearElementsCount => analysisResult?.AnalysisData?.UnclearElements?.Count ?? 0;
        private int suggestedRelationshipsCount => analysisResult?.AnalysisData?.SuggestedRelationships?.Count ?? 0;

        //relationships
        private bool showRelationshipDialog = false;
        private List<Relationship> relationshipsForSelectedTable = new List<Relationship>();
        private Relationship editingRelationship = null;
        private bool isNewRelationship = false;
        private Dictionary<int, string> tableNameLookup = new Dictionary<int, string>();
        private Dictionary<int, List<Column>> tableColumnsLookup = new Dictionary<int, List<Column>>();
        private bool isSavingRelationship = false;
        private string relationshipErrorMessage = string.Empty;

        private List<Relationship> allDatabaseRelationships = new List<Relationship>();
        private Relationship editingRelationshipInline = null;
        private Dictionary<int, string> sourceColumnOptions = new Dictionary<int, string>();
        private Dictionary<int, string> targetColumnOptions = new Dictionary<int, string>();



        protected override async Task OnInitializedAsync()
        {
            isLoading = true;
            loadingMessage = "Loading database metadata...";

            try
            {
                // Use the DatabaseId from the route parameter instead of hardcoding
                // Remove the line: DatabaseId = 5;
                //	DatabaseId = 5;
                // Get database info
                var database = await Http.GetFromJsonAsync<Database>($"api/databases/{DatabaseId}");
                databaseName = database?.Name;

                // Get tables
                tables = await Http.GetFromJsonAsync<List<Table>>($"api/tables/database/{DatabaseId}") ?? new List<Table>();

                foreach (var table in tables)
                {
                    var tableColumns = await Http.GetFromJsonAsync<List<Column>>($"api/columns/table/{table.TableID}") ?? new List<Column>();
                    columns.AddRange(tableColumns);

                    var tableRelationships = await Http.GetFromJsonAsync<List<Relationship>>($"api/relationships/table/{table.TableID}") ?? new List<Relationship>();
                    relationships.AddRange(tableRelationships);
                }

                if (tables.Count > 0)
                {
                    selectedTable = tables[0];
                }
            }
            catch (Exception ex)
            {
                await JSRuntime.InvokeVoidAsync("alert", $"Error loading database metadata: {ex.Message}");
            }
            finally
            {
                isLoading = false;
            }
        }


        private List<Column> GetColumnsForSelectedTable()
        {
            if (selectedTable == null)
                return new List<Column>();

            return columns.Where(c => c.TableID == selectedTable.TableID).ToList();
        }

        private int GetColumnCount(int tableId)
        {
            return columns.Count(c => c.TableID == tableId);
        }

        private void SelectTable(Table table)
        {
            selectedTable = table;
            currentView = "tables";
        }

        private void ShowTableList()
        {
            currentView = "tables";
        }

        private void ShowConflicts()
        {
            currentView = "conflicts";
        }

        private void ShowUnclearElements()
        {
            currentView = "unclearElements";
        }

        private void ShowSuggestedRelationships()
        {
            currentView = "suggestedRelationships";
        }

        private async Task RunSchemaAnalysis()
        {
            isAnalyzing = true;

            try
            {
                DatabaseId = 5;
                analysisResult = await Http.GetFromJsonAsync<SchemaAnalysisResult>($"api/schemaanalysis/analyze/{DatabaseId}");
                if (analysisResult?.Success == false)
                {
                    await JSRuntime.InvokeVoidAsync("alert", $"Error analyzing schema: {analysisResult.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                await JSRuntime.InvokeVoidAsync("alert", $"Error analyzing schema: {ex.Message}");
            }
            finally
            {
                isAnalyzing = false;
            }
        }

        private TableDescription GetTableAnalysis()
        {
            if (selectedTable == null || analysisResult?.AnalysisData?.TableDescriptions == null)
                return null;

            return analysisResult.AnalysisData.TableDescriptions
                .FirstOrDefault(t => t.TableName.Equals(selectedTable.DBTableName, StringComparison.OrdinalIgnoreCase));
        }

        private TableDescription tableAnalysis => GetTableAnalysis();

        private ColumnDescription GetColumnAnalysis(Column column)
        {
            if (column == null || selectedTable == null || analysisResult?.AnalysisData?.ColumnDescriptions == null)
                return null;

            return analysisResult.AnalysisData.ColumnDescriptions
                .FirstOrDefault(c =>
                    c.TableName.Equals(selectedTable.DBTableName, StringComparison.OrdinalIgnoreCase) &&
                    c.ColumnName.Equals(column.DBColumnName, StringComparison.OrdinalIgnoreCase));
        }

        private void ApplyTableSuggestion()
        {
            if (selectedTable != null && tableAnalysis != null)
            {
                selectedTable.AdminTableName = tableAnalysis.SuggestedName;
                selectedTable.AdminDescription = tableAnalysis.SuggestedDescription;
            }
        }

        private void ShowColumnSuggestion(Column column, ColumnDescription analysis)
        {
            selectedColumn = column;
            selectedColumnAnalysis = analysis;
            isColumnSuggestionModalOpen = true;
        }

        private void CloseColumnSuggestionModal()
        {
            isColumnSuggestionModalOpen = false;
        }

        private void ApplyColumnSuggestion()
        {
            if (selectedColumn != null && selectedColumnAnalysis != null)
            {
                selectedColumn.AdminColumnName = selectedColumnAnalysis.SuggestedName;
                selectedColumn.AdminDescription = selectedColumnAnalysis.SuggestedDescription;
                selectedColumn.IsLookupColumn = selectedColumnAnalysis.IsLookupColumn;

                isColumnSuggestionModalOpen = false;
            }
        }

        private async Task SaveTableChanges()
        {
            if (selectedTable == null)
                return;

            try
            {
                await Http.PutAsJsonAsync($"api/tables/{selectedTable.TableID}", selectedTable);
                await JSRuntime.InvokeVoidAsync("alert", "Table changes saved successfully!");
            }
            catch (Exception ex)
            {
                await JSRuntime.InvokeVoidAsync("alert", $"Error saving table changes: {ex.Message}");
            }
        }

        private async Task SaveColumnChanges()
        {
            if (selectedTable == null)
                return;

            var tableColumns = GetColumnsForSelectedTable();

            try
            {
                foreach (var column in tableColumns)
                {
                    await Http.PutAsJsonAsync($"api/columns/{column.ColumnID}", column);
                }

                await JSRuntime.InvokeVoidAsync("alert", "Column changes saved successfully!");
            }
            catch (Exception ex)
            {
                await JSRuntime.InvokeVoidAsync("alert", $"Error saving column changes: {ex.Message}");
            }
        }

        private async Task ApplyConflictResolution(string type, ConflictItem item)
        {
            try
            {
                if (type == "Table")
                {
                    var table = tables.FirstOrDefault(t => t.DBTableName.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
                    if (table != null)
                    {
                        table.AdminTableName = item.SuggestedResolution;
                        await Http.PutAsJsonAsync($"api/tables/{table.TableID}", table);
                        await JSRuntime.InvokeVoidAsync("alert", "Table conflict resolution applied successfully!");
                    }
                }
                else if (type == "Column")
                {
                    var tableObj = tables.FirstOrDefault(t => t.DBTableName.Equals(item.TableName, StringComparison.OrdinalIgnoreCase));
                    if (tableObj != null)
                    {
                        var column = columns.FirstOrDefault(c =>
                            c.TableID == tableObj.TableID &&
                            c.DBColumnName.Equals(item.Name, StringComparison.OrdinalIgnoreCase));

                        if (column != null)
                        {
                            column.AdminColumnName = item.SuggestedResolution;
                            await Http.PutAsJsonAsync($"api/columns/{column.ColumnID}", column);
                            await JSRuntime.InvokeVoidAsync("alert", "Column conflict resolution applied successfully!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await JSRuntime.InvokeVoidAsync("alert", $"Error applying conflict resolution: {ex.Message}");
            }
        }

        private async Task ApplyUnclearElementSuggestion(UnclearElement element)
        {
            try
            {
                if (element.Type == "Table")
                {
                    var table = tables.FirstOrDefault(t => t.DBTableName.Equals(element.Name, StringComparison.OrdinalIgnoreCase));
                    if (table != null)
                    {
                        table.AdminDescription = element.Suggestion;
                        await Http.PutAsJsonAsync($"api/tables/{table.TableID}", table);
                        await JSRuntime.InvokeVoidAsync("alert", "Table suggestion applied successfully!");
                    }
                }
                else if (element.Type == "Column")
                {
                    var tableObj = tables.FirstOrDefault(t => t.DBTableName.Equals(element.TableName, StringComparison.OrdinalIgnoreCase));
                    if (tableObj != null)
                    {
                        var column = columns.FirstOrDefault(c =>
                            c.TableID == tableObj.TableID &&
                            c.DBColumnName.Equals(element.Name, StringComparison.OrdinalIgnoreCase));

                        if (column != null)
                        {
                            column.AdminDescription = element.Suggestion;
                            await Http.PutAsJsonAsync($"api/columns/{column.ColumnID}", column);
                            await JSRuntime.InvokeVoidAsync("alert", "Column suggestion applied successfully!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await JSRuntime.InvokeVoidAsync("alert", $"Error applying suggestion: {ex.Message}");
            }
        }

        private async Task AddSuggestedRelationship(SuggestedRelationship relationship)
        {
            try
            {
                DatabaseId = 5;
                await Http.PostAsJsonAsync($"api/schemaanalysis/add-relationship/{DatabaseId}", relationship);
                await JSRuntime.InvokeVoidAsync("alert", "Relationship added successfully!");

                // Refresh relationships
                relationships.Clear();
                foreach (var table in tables)
                {
                    var tableRelationships = await Http.GetFromJsonAsync<List<Relationship>>($"api/relationships/table/{table.TableID}") ?? new List<Relationship>();
                    relationships.AddRange(tableRelationships);
                }
            }
            catch (Exception ex)
            {
                await JSRuntime.InvokeVoidAsync("alert", $"Error adding relationship: {ex.Message}");
            }
        }

        private async Task ApplyAllSuggestions()
        {
            if (!hasAnalysisResults)
                return;

            isApplying = true;

            try
            {
                DatabaseId = 5;
                // Send the entire analysis data to be applied
                await Http.PostAsJsonAsync($"api/schemaanalysis/apply-descriptions/{DatabaseId}", analysisResult.AnalysisData);

                // Refresh data
                await OnInitializedAsync();

                await JSRuntime.InvokeVoidAsync("alert", "All suggestions applied successfully!");
            }
            catch (Exception ex)
            {
                await JSRuntime.InvokeVoidAsync("alert", $"Error applying all suggestions: {ex.Message}");
            }
            finally
            {
                isApplying = false;
            }
        }

        [JSInvokable]
        public void OnTableClick(string tableId)
        {
            // Extract numeric ID from the string (it's in format "table_123")
            if (int.TryParse(tableId.Replace("table_", ""), out int id))
            {
                NavigationManager.NavigateTo($"/admin/edit-table/{id}");
            }
        }

        [JSInvokable]
        public void OnColumnClick(string columnId)
        {
            if (int.TryParse(columnId.Replace("column_", ""), out int id))
            {
                NavigationManager.NavigateTo($"/admin/edit-column/{id}");
            }
        }

        [JSInvokable]
        public void OnRelationshipClick(string relationshipId)
        {
            if (int.TryParse(relationshipId.Replace("rel_", ""), out int id))
            {
                NavigationManager.NavigateTo($"/admin/edit-relationship/{id}");
            }
        }


        //relationships

        // Add a method to handle relationship management
        private async Task ManageRelationships()
        {
            if (selectedTable == null)
                return;

            // Retrieve relationships for the selected table
            var tableRelationships = await Http.GetFromJsonAsync<List<Relationship>>($"api/relationships/table/{selectedTable.TableID}") ?? new List<Relationship>();

            // Show relationship management dialog
            showRelationshipDialog = true;
            relationshipsForSelectedTable = tableRelationships;


        }


        private async Task ShowRelationshipManager()
        {
            try
            {
                isLoading = true;

                // Prepare lookup tables for display
                tableNameLookup = tables.ToDictionary(t => t.TableID, t => t.DBTableName);

                // Load ALL relationships for the database
                allDatabaseRelationships = new List<Relationship>();
                foreach (var table in tables)
                {
                    var tableRelationships = await Http.GetFromJsonAsync<List<Relationship>>($"api/relationships/table/{table.TableID}") ?? new List<Relationship>();
                    // Filter out duplicates (since relationships appear for both source and target tables)
                    foreach (var rel in tableRelationships)
                    {
                        if (!allDatabaseRelationships.Any(r => r.RelationshipID == rel.RelationshipID))
                        {
                            allDatabaseRelationships.Add(rel);
                        }
                    }
                }

                // Also set the relationshipsForSelectedTable for backward compatibility
                relationshipsForSelectedTable = selectedTable != null
                    ? allDatabaseRelationships.Where(r => r.TableID == selectedTable.TableID || r.RelatedTableID == selectedTable.TableID).ToList()
                    : new List<Relationship>();

                // Prepare columns for selection
                await PrepareTableColumnsLookup();

                // Show the dialog
                showRelationshipDialog = true;
            }
            catch (Exception ex)
            {
                await JSRuntime.InvokeVoidAsync("alert", $"Error loading relationships: {ex.Message}");
            }
            finally
            {
                isLoading = false;
            }
        }

        // Get table name by ID
        private string GetTableName(int tableId)
        {
            var table = tables.FirstOrDefault(t => t.TableID == tableId);
            return table?.DBTableName ?? $"Table ID {tableId}";
        }

        // Get column name by IDs
        private string GetColumnName(int tableId, int columnId)
        {
            if (tableColumnsLookup.ContainsKey(tableId))
            {
                var column = tableColumnsLookup[tableId].FirstOrDefault(c => c.ColumnID == columnId);
                return column?.DBColumnName ?? $"Column ID {columnId}";
            }
            return $"Column ID {columnId}";
        }


        // Helper method to load all table columns for relationship setup
        private async Task PrepareTableColumnsLookup()
        {
            tableColumnsLookup.Clear();

            foreach (var table in tables)
            {
                if (!tableColumnsLookup.ContainsKey(table.TableID))
                {
                    var tableColumns = await Http.GetFromJsonAsync<List<Column>>($"api/columns/table/{table.TableID}") ?? new List<Column>();
                    tableColumnsLookup[table.TableID] = tableColumns;
                }
            }
        }

        // Methods for inline editing
        private void EditInlineRelationship(Relationship relationship)
        {
            // Clone the relationship for editing
            editingRelationshipInline = new Relationship
            {
                RelationshipID = relationship.RelationshipID,
                TableID = relationship.TableID,
                ColumnID = relationship.ColumnID,
                RelatedTableID = relationship.RelatedTableID,
                RelatedColumnID = relationship.RelatedColumnID,
                RelationshipType = relationship.RelationshipType,
                Description = relationship.Description,
                IsEnforced = relationship.IsEnforced,
                CreatedAt = relationship.CreatedAt,
                CreatedBy = relationship.CreatedBy
            };

            // Prepare column options for dropdowns
            UpdateSourceColumnOptions();
            UpdateTargetColumnOptions();
        }

        private void CancelInlineEditing()
        {
            editingRelationshipInline = null;
        }

        private async Task SaveInlineRelationship()
        {
            if (editingRelationshipInline.TableID == 0 ||
                editingRelationshipInline.ColumnID == 0 ||
                editingRelationshipInline.RelatedTableID == 0 ||
                editingRelationshipInline.RelatedColumnID == 0)
            {
                await JSRuntime.InvokeVoidAsync("alert", "Please select source table, source column, target table, and target column");
                return;
            }

            try
            {
                // Use the existing API endpoint to save
                await Http.PutAsJsonAsync($"api/relationships/{editingRelationshipInline.RelationshipID}", editingRelationshipInline);

                // Update the relationship in the list
                var index = allDatabaseRelationships.FindIndex(r => r.RelationshipID == editingRelationshipInline.RelationshipID);
                if (index >= 0)
                {
                    allDatabaseRelationships[index] = editingRelationshipInline;
                }

                // Clear editing state
                editingRelationshipInline = null;

                await JSRuntime.InvokeVoidAsync("alert", "Relationship updated successfully!");
            }
            catch (Exception ex)
            {
                await JSRuntime.InvokeVoidAsync("alert", $"Error saving relationship: {ex.Message}");
            }
        }



        private async Task UpdateSourceColumnOptions()
        {
            sourceColumnOptions = new Dictionary<int, string>();

            if (editingRelationship?.TableID > 0)
            {
                // Ensure columns are loaded for this table
                if (!tableColumnsLookup.ContainsKey(editingRelationship.TableID))
                {
                    await LoadColumnsForTable(editingRelationship.TableID);
                }

                if (tableColumnsLookup.ContainsKey(editingRelationship.TableID))
                {
                    foreach (var column in tableColumnsLookup[editingRelationship.TableID])
                    {
                        sourceColumnOptions[column.ColumnID] = column.DBColumnName;
                    }
                }
            }

            StateHasChanged(); // Force UI update
        }

        private async Task UpdateTargetColumnOptions()
        {
            targetColumnOptions = new Dictionary<int, string>();

            if (editingRelationship?.RelatedTableID > 0)
            {
                // Ensure columns are loaded for this table
                if (!tableColumnsLookup.ContainsKey(editingRelationship.RelatedTableID))
                {
                    await LoadColumnsForTable(editingRelationship.RelatedTableID);
                }

                if (tableColumnsLookup.ContainsKey(editingRelationship.RelatedTableID))
                {
                    foreach (var column in tableColumnsLookup[editingRelationship.RelatedTableID])
                    {
                        targetColumnOptions[column.ColumnID] = column.DBColumnName;
                    }
                }
            }

            StateHasChanged(); // Force UI update
        }

        // Helper method to load columns for a specific table
        private async Task LoadColumnsForTable(int tableId)
        {
            try
            {
                var tableColumns = await Http.GetFromJsonAsync<List<Column>>($"api/columns/table/{tableId}") ?? new List<Column>();
                tableColumnsLookup[tableId] = tableColumns;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading columns for table {tableId}: {ex.Message}");

            }
        }

        // Method to create a new relationship
        private async Task CreateNewRelationship()
        {
            isNewRelationship = true;

            editingRelationship = new Relationship
            {
                TableID = selectedTable?.TableID ?? tables.FirstOrDefault()?.TableID ?? 0,
                RelatedTableID = 0, // User must select
                ColumnID = 0,       // User must select
                RelatedColumnID = 0, // User must select
                RelationshipType = "One-to-Many", // Default value
                Description = "",
                IsEnforced = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 3 //temp
            };

            // Pre-load columns for the default selected table
            await UpdateSourceColumnOptions();

            // For UI update
            StateHasChanged();
        }

        // Method to edit existing relationship
        private void EditRelationship(Relationship relationship)
        {
            isNewRelationship = false;
            editingRelationship = new Relationship
            {
                RelationshipID = relationship.RelationshipID,
                TableID = relationship.TableID,
                ColumnID = relationship.ColumnID,
                RelatedTableID = relationship.RelatedTableID,
                RelatedColumnID = relationship.RelatedColumnID,
                RelationshipType = relationship.RelationshipType,
                Description = relationship.Description,
                IsEnforced = relationship.IsEnforced,
                CreatedAt = relationship.CreatedAt,
                CreatedBy = relationship.CreatedBy
            };
        }

        // Method to save a relationship
        private async Task SaveRelationship()
        {
            relationshipErrorMessage = string.Empty;

            if (editingRelationship.RelatedTableID == 0 ||
                editingRelationship.ColumnID == 0 ||
                editingRelationship.RelatedColumnID == 0)
            {
                relationshipErrorMessage = "Please select source column, target table and target column";
                return;
            }

            try
            {
                isSavingRelationship = true;

                if (isNewRelationship)
                {
                    await Http.PostAsJsonAsync("api/relationships", editingRelationship);
                }
                else
                {
                    await Http.PutAsJsonAsync($"api/relationships/{editingRelationship.RelationshipID}", editingRelationship);
                }

                // Refresh relationships after save
                relationshipsForSelectedTable = await Http.GetFromJsonAsync<List<Relationship>>($"api/relationships/table/{selectedTable.TableID}") ?? new List<Relationship>();

                // Close the editing form
                CloseRelationshipEditor();

                // Display success message
                await JSRuntime.InvokeVoidAsync("alert", isNewRelationship ?
                    "Relationship created successfully!" :
                    "Relationship updated successfully!");
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

        // Method to delete a relationship
        private async Task DeleteRelationship(int relationshipId)
        {
            try
            {
                if (!await JSRuntime.InvokeAsync<bool>("confirm", "Are you sure you want to delete this relationship?"))
                    return;

                await Http.DeleteAsync($"api/relationships/{relationshipId}");

                // Remove from current list
                relationshipsForSelectedTable.RemoveAll(r => r.RelationshipID == relationshipId);

                await JSRuntime.InvokeVoidAsync("alert", "Relationship deleted successfully!");
            }
            catch (Exception ex)
            {
                await JSRuntime.InvokeVoidAsync("alert", $"Error deleting relationship: {ex.Message}");
            }
        }

        // Method to close the relationship editor
        private void CloseRelationshipEditor()
        {
            editingRelationship = null;
        }

        // Method to close the relationship dialog
        private void CloseRelationshipDialog()
        {
            showRelationshipDialog = false;
            editingRelationship = null;
        }

        // Helper method to get table and column names for display
        private string GetTableColumnName(int tableId, int columnId)
        {
            string tableName = tableNameLookup.ContainsKey(tableId) ? tableNameLookup[tableId] : $"Table {tableId}";

            if (tableColumnsLookup.ContainsKey(tableId))
            {
                var column = tableColumnsLookup[tableId].FirstOrDefault(c => c.ColumnID == columnId);
                if (column != null)
                {
                    return $"{tableName}.{column.DBColumnName}";
                }
            }

            return $"{tableName}.Column {columnId}";
        }

  
    }
}
