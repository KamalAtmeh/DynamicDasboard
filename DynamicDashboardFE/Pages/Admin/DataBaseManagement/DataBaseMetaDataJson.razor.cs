using DynamicDashboardCommon.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace DynamicDashboardFE.Pages.Admin
{
    public partial class DataBaseMetaDataJson : ComponentBase
    {
        [Parameter] public int DatabaseId { get; set; }

        private string databaseName;
        private bool isLoading = true;
        private bool isAnalyzing = false;
        private bool isApplying = false;
        private string loadingMessage = "Loading database metadata...";
        private string currentView = "tables";

        // Full JSON schema loaded from the server
        private DatabaseSchema currentSchema = new DatabaseSchema();
        private TableSchema selectedTable;

        // For column suggestion
        private ColumnSchema selectedColumn;
        private ColumnDescription selectedColumnAnalysis;
        private bool isColumnSuggestionModalOpen = false;

        // For relationship inline editing
        private RelationshipSchema editingRelationship = null;

        // Analysis results reference
        private AnalysisResults analysisResult => currentSchema.AnalysisResults;

        // Helper booleans for enabling/disabling UI elements
        private bool hasAnalysisResults => analysisResult != null;
        private bool hasConflicts => analysisResult != null && analysisResult.PotentialConflicts != null && analysisResult.PotentialConflicts.Count > 0;
        private bool hasUnclearElements => analysisResult != null && analysisResult.UnclearElements != null && analysisResult.UnclearElements.Count > 0;
        private bool hasSuggestedRelationships => analysisResult != null && analysisResult.SuggestedRelationships != null && analysisResult.SuggestedRelationships.Count > 0;

        private int conflictCount => analysisResult?.PotentialConflicts?.Count ?? 0;
        private int unclearElementsCount => analysisResult?.UnclearElements?.Count ?? 0;
        private int suggestedRelationshipsCount => analysisResult?.SuggestedRelationships?.Count ?? 0;

        // For synonyms management
        private string newTableSynonym = string.Empty;
        private string newColumnSynonym = string.Empty;
        private bool isColumnSynonymsModalOpen = false;
        private List<string> tempColumnSynonyms = new List<string>();

        protected override async Task OnInitializedAsync()
        {
            await LoadSchemaJson();
        }

        private async Task LoadSchemaJson()
        {
            try
            {
                isLoading = true;
                loadingMessage = "Loading database metadata...";

                // Use the API to get the schema
                var response = await Http.GetAsync($"api/DatabaseSchema/GetSchema/{DatabaseId}");

                if (response.IsSuccessStatusCode)
                {
                    var dbJsonSchema = await response.Content.ReadFromJsonAsync<DatabaseSchema>();


                    if (dbJsonSchema != null && !string.IsNullOrWhiteSpace(dbJsonSchema.SchemaData))
                    {
                        // The schema exists - deserialize it
                        currentSchema = await Http.GetFromJsonAsync<DatabaseSchema>($"api/DatabaseSchema/parsed/{DatabaseId}");
                        databaseName = currentSchema.Name;
                    }
                    else
                    {
                        // No schema exists yet - create a minimal one
                        await JSRuntime.InvokeVoidAsync("alert", $"Error loading schema");
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Schema doesn't exist - create a minimal one
                    await JSRuntime.InvokeVoidAsync("alert", $"Error loading schema");
                }
                else
                {
                    throw new Exception($"Error loading schema: {response.StatusCode}");
                }

                // If we have tables, default to the first one
                if (currentSchema.Tables != null && currentSchema.Tables.Count > 0)
                {
                    selectedTable = currentSchema.Tables[0];
                }
            }
            catch (Exception ex)
            {
                await JSRuntime.InvokeVoidAsync("alert", $"Error loading schema JSON: {ex.Message}");
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private async Task SaveSchemaJson()
        {
            try
            {
                isLoading = true;

                // Re-serialize the entire schema
                string schemaData = JsonSerializer.Serialize(currentSchema, new JsonSerializerOptions { WriteIndented = true });
                var schemaToSave = new DatabaseSchema
                {
                    ID = DatabaseId,
                    Name = databaseName,
                    Status = 1,
                    SchemaData = schemaData
                };

                HttpResponseMessage response;

                response = await Http.PutAsJsonAsync($"api/DatabaseSchema/{DatabaseId}", schemaToSave);


                if (response.IsSuccessStatusCode)
                {
                    await JSRuntime.InvokeVoidAsync("alert", "Schema saved successfully!");
                }
                else
                {
                    await JSRuntime.InvokeVoidAsync("alert", "Error saving schema.");
                }
            }
            catch (Exception ex)
            {
                await JSRuntime.InvokeVoidAsync("alert", $"Error saving schema");
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        // Table / Column methods
        private void SelectTable(TableSchema table)
        {
            selectedTable = table;
            currentView = "tables";
        }

        private List<ColumnSchema> GetColumnsForSelectedTable()
        {
            if (selectedTable == null)
                return new List<ColumnSchema>();

            return selectedTable.Columns ?? new List<ColumnSchema>();
        }

        private int GetColumnCount(TableSchema table)
        {
            return table.Columns?.Count ?? 0;
        }

        private async Task SaveTableChanges()
        {
            if (selectedTable == null)
                return;
            await SaveSchemaJson();
        }

        private async Task SaveColumnChanges()
        {
            if (selectedTable == null)
                return;
            await SaveSchemaJson();
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
        }

        private void ApplyColumnSuggestion()
        {
            if (selectedColumn != null && selectedColumnAnalysis != null)
            {
                selectedColumn.FriendlyName = selectedColumnAnalysis.SuggestedName;
                selectedColumn.Description = selectedColumnAnalysis.SuggestedDescription;
                selectedColumn.IsLookup = selectedColumnAnalysis.IsLookupColumn;
                isColumnSuggestionModalOpen = false;
            }
        }

        // Relationship management
        private async Task ShowRelationshipManager()
        {
            currentView = "relationships";
        }

        private void CreateNewRelationship()
        {
            editingRelationship = new RelationshipSchema
            {
                ID = Guid.NewGuid().ToString(),
                Name = "",
                Type = "one-to-many",
                Status = "active",
                Enforced = false,
                Source = new RelationshipDetails { TableID = "", ColumnID = "" },
                Target = new RelationshipDetails { TableID = "", ColumnID = "" },
                Metadata = new RelationshipMetadata
                {
                    Confidence = 0.0,
                    DiscoveredAt = DateTime.UtcNow,
                    LastValidated = DateTime.UtcNow
                }
            };
            currentView = "relationships";
        }

        private void EditInlineRelationship(RelationshipSchema rel)
        {
            // Deep copy the existing relationship for editing
            editingRelationship = JsonSerializer.Deserialize<RelationshipSchema>(JsonSerializer.Serialize(rel));
        }

        private void CancelInlineEditing()
        {
            editingRelationship = null;
        }

        // When editing a relationship, also update the friendly names
        private async Task SaveInlineRelationship()
        {
            if (editingRelationship == null) return;

            // Update friendly names before saving
            var sourceTable = currentSchema.Tables.FirstOrDefault(t => t.ID == editingRelationship.Source.TableID);
            var sourceColumn = sourceTable?.Columns?.FirstOrDefault(c => c.ID == editingRelationship.Source.ColumnID);
            var targetTable = currentSchema.Tables.FirstOrDefault(t => t.ID == editingRelationship.Target.TableID);
            var targetColumn = targetTable?.Columns?.FirstOrDefault(c => c.ID == editingRelationship.Target.ColumnID);

            if (sourceTable != null && sourceColumn != null && targetTable != null && targetColumn != null)
            {
                editingRelationship.Source.TableName = !string.IsNullOrEmpty(sourceTable.FriendlyName) ? sourceTable.FriendlyName : sourceTable.DBName;
                editingRelationship.Source.ColumnName = !string.IsNullOrEmpty(sourceColumn.FriendlyName) ? sourceColumn.FriendlyName : sourceColumn.DBName;
                editingRelationship.Target.TableName = !string.IsNullOrEmpty(targetTable.FriendlyName) ? targetTable.FriendlyName : targetTable.DBName;
                editingRelationship.Target.ColumnName = !string.IsNullOrEmpty(targetColumn.FriendlyName) ? targetColumn.FriendlyName : targetColumn.DBName;

                var index = currentSchema.Relationships.FindIndex(r => r.ID == editingRelationship.ID);
                if (index >= 0)
                {
                    currentSchema.Relationships[index] = editingRelationship;
                }
                else
                {
                    currentSchema.Relationships.Add(editingRelationship);
                }
                editingRelationship = null;
                await SaveSchemaJson();
            }
            else
            {
                await JSRuntime.InvokeVoidAsync("alert", "Error: Could not find all table or column references.");
            }
        }

        private async Task DeleteRelationship(string relationshipId)
        {
            try
            {
                var confirm = await JSRuntime.InvokeAsync<bool>("confirm", "Are you sure you want to delete this relationship?");
                if (!confirm) return;

                currentSchema.Relationships.RemoveAll(r => r.ID == relationshipId);
                await SaveSchemaJson();
                await JSRuntime.InvokeVoidAsync("alert", "Relationship deleted successfully!");
            }
            catch (Exception ex)
            {
                await JSRuntime.InvokeVoidAsync("alert", $"Error deleting relationship: {ex.Message}");
            }
        }

        // Conflicts, Unclear elements, and Suggested relationships
        private void ShowConflicts() => currentView = "conflicts";
        private void ShowUnclearElements() => currentView = "unclearElements";
        private void ShowSuggestedRelationships() => currentView = "suggestedRelationships";

        private async Task RunSchemaAnalysis()
        {
            isAnalyzing = true;
            try
            {
                // Example: calling a hypothetical endpoint for analyzing the schema
                var analysis = await Http.GetFromJsonAsync<AnalysisResults>($"api/schemaanalysis/analyze/{DatabaseId}");
                if (analysis != null)
                {
                    currentSchema.AnalysisResults = analysis;
                }
                else
                {
                    await JSRuntime.InvokeVoidAsync("alert", "Error analyzing schema or no data returned.");
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

        private async Task ApplyAllSuggestions()
        {
            if (analysisResult == null)
                return;

            isApplying = true;
            try
            {
                // Example: calling a hypothetical endpoint to apply suggestions
                var response = await Http.PostAsJsonAsync($"api/schemaanalysis/apply-descriptions/{DatabaseId}", analysisResult);
                if (response.IsSuccessStatusCode)
                {
                    // Reload after applying suggestions
                    await LoadSchemaJson();
                    await JSRuntime.InvokeVoidAsync("alert", "All suggestions applied successfully!");
                }
                else
                {
                    await JSRuntime.InvokeVoidAsync("alert", "Error applying suggestions.");
                }
            }
            catch (Exception ex)
            {
                await JSRuntime.InvokeVoidAsync("alert", $"Error applying suggestions: {ex.Message}");
            }
            finally
            {
                isApplying = false;
            }
        }

        private void ShowTableList()
        {
            currentView = "tables";
        }

        // Conflict resolution
        private async Task ApplyConflictResolution(string type, ConflictItem item)
        {
            try
            {
                // Example logic: if conflict is a column rename, we rename in memory
                // Then call SaveSchemaJson
                if (type == "Column")
                {
                    // find table, find column, rename
                    var tableObj = currentSchema.Tables?.Find(t => t.DBName.Equals(item.TableName, StringComparison.OrdinalIgnoreCase));
                    if (tableObj != null && tableObj.Columns != null)
                    {
                        var col = tableObj.Columns.Find(c => c.DBName.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
                        if (col != null)
                        {
                            col.FriendlyName = item.SuggestedResolution;
                        }
                    }
                }
                else if (type == "Table")
                {
                    // find table, rename
                    var tableObj = currentSchema.Tables?.Find(t => t.DBName.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
                    if (tableObj != null)
                    {
                        tableObj.FriendlyName = item.SuggestedResolution;
                    }
                }
                await SaveSchemaJson();
                await JSRuntime.InvokeVoidAsync("alert", "Conflict resolution applied!");
            }
            catch (Exception ex)
            {
                await JSRuntime.InvokeVoidAsync("alert", $"Error applying conflict resolution: {ex.Message}");
            }
        }

        // Unclear element resolution
        private async Task ApplyUnclearElementSuggestion(UnclearElement element)
        {
            try
            {
                if (element.Type == "Column")
                {
                    var tableObj = currentSchema.Tables?.Find(t => t.DBName.Equals(element.TableName, StringComparison.OrdinalIgnoreCase));
                    if (tableObj != null && tableObj.Columns != null)
                    {
                        var col = tableObj.Columns.Find(c => c.DBName.Equals(element.Name, StringComparison.OrdinalIgnoreCase));
                        if (col != null)
                        {
                            col.Description = element.Suggestion;
                        }
                    }
                }
                else if (element.Type == "Table")
                {
                    var tableObj = currentSchema.Tables?.Find(t => t.DBName.Equals(element.Name, StringComparison.OrdinalIgnoreCase));
                    if (tableObj != null)
                    {
                        tableObj.Description = element.Suggestion;
                    }
                }
                await SaveSchemaJson();
                await JSRuntime.InvokeVoidAsync("alert", "Suggestion applied!");
            }
            catch (Exception ex)
            {
                await JSRuntime.InvokeVoidAsync("alert", $"Error applying suggestion: {ex.Message}");
            }
        }

        // Adding suggested relationship
        private async Task AddSuggestedRelationship(SuggestedRelationship relationship)
        {
            try
            {
                // Convert suggested relationship to a normal RelationshipSchema, add to currentSchema
                var newRel = new RelationshipSchema
                {
                    ID = Guid.NewGuid().ToString(),
                    Name = "SuggestedRel",
                    Type = relationship.RelationshipType,
                    Status = "active",
                    Enforced = false,
                    Source = new RelationshipDetails
                    {
                        TableID = relationship.SourceTable.TableID,
                        ColumnID = relationship.SourceTable.ColumnID
                    },
                    Target = new RelationshipDetails
                    {
                        TableID = relationship.TargetTable.TableID,
                        ColumnID = relationship.TargetTable.ColumnID
                    },
                    Metadata = new RelationshipMetadata
                    {
                        Confidence = relationship.Confidence,
                        DiscoveredAt = DateTime.UtcNow,
                        LastValidated = DateTime.UtcNow
                    }
                };
                if (currentSchema.Relationships == null)
                    currentSchema.Relationships = new List<RelationshipSchema>();
                currentSchema.Relationships.Add(newRel);

                await SaveSchemaJson();
                await JSRuntime.InvokeVoidAsync("alert", "Suggested relationship added!");
            }
            catch (Exception ex)
            {
                await JSRuntime.InvokeVoidAsync("alert", $"Error adding relationship: {ex.Message}");
            }
        }

        // Table suggestion
        private void ApplyTableSuggestion()
        {
            if (selectedTable == null || tableAnalysis == null)
                return;

            selectedTable.FriendlyName = tableAnalysis.SuggestedName;
            selectedTable.Description = tableAnalysis.SuggestedDescription;
        }

        // Retrieve analysis info for the selected table
        private TableDescription tableAnalysis => analysisResult?.TableDescriptions?
            .Find(t => t.TableName.Equals(selectedTable?.DBName, StringComparison.OrdinalIgnoreCase));

        // Retrieve analysis info for a column
        private ColumnDescription GetColumnAnalysis(ColumnSchema column)
        {
            if (analysisResult == null || analysisResult.ColumnDescriptions == null || column == null || selectedTable == null)
                return null;

            return analysisResult.ColumnDescriptions.Find(cd =>
                cd.TableName.Equals(selectedTable.DBName, StringComparison.OrdinalIgnoreCase)
                && cd.ColumnName.Equals(column.DBName, StringComparison.OrdinalIgnoreCase));
        }

        // Navigation methods
        [JSInvokable]
        public void OnTableClick(string tableId)
        {
            // Example code
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

        // For synonyms management


        private void AddTableSynonym()
        {
            if (string.IsNullOrWhiteSpace(newTableSynonym) || selectedTable == null)
                return;

            if (selectedTable.Synonyms == null)
                selectedTable.Synonyms = new List<string>();

            if (!selectedTable.Synonyms.Contains(newTableSynonym, StringComparer.OrdinalIgnoreCase))
                selectedTable.Synonyms.Add(newTableSynonym);

            newTableSynonym = string.Empty;
        }

        private void RemoveTableSynonym(string synonym)
        {
            if (selectedTable?.Synonyms == null)
                return;

            selectedTable.Synonyms.Remove(synonym);
        }

        private void ShowColumnSynonymsModal(ColumnSchema column)
        {
            selectedColumn = column;

            // Initialize synonyms list if null
            if (selectedColumn.Synonyms == null)
                selectedColumn.Synonyms = new List<string>();

            // Create a temporary copy for editing
            tempColumnSynonyms = new List<string>(selectedColumn.Synonyms);

            isColumnSynonymsModalOpen = true;
        }

        private void CloseColumnSynonymsModal()
        {
            isColumnSynonymsModalOpen = false;
        }

        private void AddColumnSynonym()
        {
            if (string.IsNullOrWhiteSpace(newColumnSynonym) || selectedColumn == null)
                return;

            if (!tempColumnSynonyms.Contains(newColumnSynonym, StringComparer.OrdinalIgnoreCase))
                tempColumnSynonyms.Add(newColumnSynonym);

            newColumnSynonym = string.Empty;
        }

        private void RemoveColumnSynonym(string synonym)
        {
            tempColumnSynonyms.Remove(synonym);
        }

        private void SaveColumnSynonyms()
        {
            if (selectedColumn != null)
            {
                selectedColumn.Synonyms = new List<string>(tempColumnSynonyms);
            }

            CloseColumnSynonymsModal();
        }

    }
}
