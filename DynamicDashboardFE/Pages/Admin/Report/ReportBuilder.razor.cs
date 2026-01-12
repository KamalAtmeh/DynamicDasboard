//using DynamicDashboardCommon.Models;

//using Microsoft.AspNetCore.Components;
//using Microsoft.AspNetCore.Components.Web;
//using Microsoft.JSInterop;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net.Http;
//using System.Net.Http.Json;
//using System.Text.Json;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;

//namespace DynamicDashboardFE.Pages.Admin
//{
//    /// <summary>
//    /// Code-behind for AI Report Builder page.
//    /// </summary>
//    public partial class ReportBuilder : ComponentBase
//    {
//        #region Injected Services

//        [Inject] private HttpClient Http { get; set; }
//        [Inject] private IJSRuntime JSRuntime { get; set; }
//        [Inject] private NavigationManager NavigationManager { get; set; }

//        #endregion

//        #region Parameters

//        [Parameter] public int? ReportId { get; set; }

//        #endregion

//        #region State - Report

//        private DynamicDashboardCommon.Models.Report currentReport;
//        private string reportTitle = "";
//        private int selectedDatabaseId = 0;
//        private List<Database> databases = new();
//        private bool isSaving = false;

//        #endregion

//        #region State - Generation

//        private string generationPrompt = "";
//        private bool isGenerating = false;

//        #endregion

//        #region State - Sections

//        private int selectedSectionId = 0;
//        private Dictionary<int, List<Dictionary<string, object>>> sectionData = new();
//        private Dictionary<int, bool> sectionLoading = new();
//        private Dictionary<int, string> sectionErrors = new();
//        private Dictionary<int, int> sectionTotalCounts = new();
//        private Dictionary<int, string> sectionSearchTerms = new();
//        private Dictionary<string, string> columnFilters = new();
//        private Dictionary<int, string> sectionSortColumn = new();
//        private Dictionary<int, bool> sectionSortAscending = new();
//        private Dictionary<int, int> sectionCurrentPage = new();
//        private Dictionary<int, int> sectionPageSize = new();
//        private int showExportDropdown = 0;

//        #endregion

//        #region State - AI Assistant

//        private bool showAIAssistant = true;
//        private List<AIChatMessage> chatMessages = new();
//        private string chatInput = "";
//        private bool isAIThinking = false;
//        private ElementReference chatHistoryRef;

//        #endregion

//        #region State - Modals

//        private bool showSqlModal = false;
//        private bool showColumnModal = false;
//        private bool showExportModal = false;
//        private bool showExplanationModal = false;
//        private ReportSection editingSection;
//        private string editingSql = "";
//        private ReportColumnConfiguration editingColumnConfig;
//        private string selectedExportFormat = "pdf";
//        private bool isTestingQuery = false;
//        private string testQueryResult = "";
//        private bool testQuerySuccess = false;
//        private bool isExplaining = false;
//        private ExplainDataResponse currentExplanation;

//        #endregion

//        #region Lifecycle Methods

//        protected override async Task OnInitializedAsync()
//        {
//            await LoadDatabases();

//            if (ReportId.HasValue && ReportId.Value > 0)
//            {
//                await LoadReport(ReportId.Value);
//            }
//        }

//        #endregion

//        #region Data Loading

//        private async Task LoadDatabases()
//        {
//            try
//            {
//                databases = await Http.GetFromJsonAsync<List<Database>>("api/database") ?? new List<Database>();
//            }
//            catch (Exception ex)
//            {
//                await ShowError($"Failed to load databases: {ex.Message}");
//            }
//        }

//        private async Task LoadReport(int reportId)
//        {
//            try
//            {
//                currentReport = await Http.GetFromJsonAsync<DynamicDashboardCommon.Models.Report.Report>($"api/report/{reportId}");
                
//                if (currentReport != null)
//                {
//                    reportTitle = currentReport.Title;
//                    selectedDatabaseId = currentReport.DatabaseID;

//                    // Initialize section states
//                    foreach (var section in currentReport.Sections ?? new List<ReportSection>())
//                    {
//                        InitializeSectionState(section.SectionID);
//                    }

//                    // Load data for all sections
//                    await LoadAllSectionData();
//                }
//            }
//            catch (Exception ex)
//            {
//                await ShowError($"Failed to load report: {ex.Message}");
//            }
//        }

//        private void InitializeSectionState(int sectionId)
//        {
//            if (!sectionSearchTerms.ContainsKey(sectionId))
//                sectionSearchTerms[sectionId] = "";
//            if (!sectionCurrentPage.ContainsKey(sectionId))
//                sectionCurrentPage[sectionId] = 1;
//            if (!sectionPageSize.ContainsKey(sectionId))
//                sectionPageSize[sectionId] = 25;
//            if (!sectionSortAscending.ContainsKey(sectionId))
//                sectionSortAscending[sectionId] = true;
//        }

//        private async Task LoadAllSectionData()
//        {
//            if (currentReport?.Sections == null) return;

//            var dataSections = currentReport.Sections
//                .Where(s => s.SectionType == ReportSectionTypeEnum.DataTable && !string.IsNullOrEmpty(s.QueryText))
//                .ToList();

//            foreach (var section in dataSections)
//            {
//                await LoadSectionData(section);
//            }
//        }

//        private async Task LoadSectionData(ReportSection section)
//        {
//            if (section == null || string.IsNullOrEmpty(section.QueryText)) return;

//            sectionLoading[section.SectionID] = true;
//            sectionErrors.Remove(section.SectionID);
//            StateHasChanged();

//            try
//            {
//                var response = await Http.GetFromJsonAsync<ExecuteSectionQueryResponse>(
//                    $"api/report/{currentReport.ReportID}/sections/{section.SectionID}/data?page=1&pageSize=1000");

//                if (response != null && response.Success)
//                {
//                    sectionData[section.SectionID] = response.Data ?? new List<Dictionary<string, object>>();
//                    sectionTotalCounts[section.SectionID] = response.TotalCount;

//                    // Auto-generate column config if not present
//                    if (string.IsNullOrEmpty(section.ColumnConfiguration) && response.Columns?.Any() == true)
//                    {
//                        var config = GenerateColumnConfig(response.Columns);
//                        section.ColumnConfiguration = JsonSerializer.Serialize(config);
//                    }
//                }
//                else
//                {
//                    sectionErrors[section.SectionID] = response?.ErrorMessage ?? "Failed to load data";
//                }
//            }
//            catch (Exception ex)
//            {
//                sectionErrors[section.SectionID] = ex.Message;
//            }
//            finally
//            {
//                sectionLoading[section.SectionID] = false;
//                StateHasChanged();
//            }
//        }

//        private async Task RefreshSectionData(ReportSection section)
//        {
//            await LoadSectionData(section);
//        }

//        #endregion

//        #region Report Generation

//        private async Task GenerateReport()
//        {
//            if (selectedDatabaseId == 0 || string.IsNullOrWhiteSpace(generationPrompt))
//                return;

//            isGenerating = true;
//            StateHasChanged();

//            try
//            {
//                var request = new GenerateReportRequest
//                {
//                    Prompt = generationPrompt,
//                    DatabaseID = selectedDatabaseId,
//                    Title = string.IsNullOrWhiteSpace(reportTitle) ? null : reportTitle,
//                    IncludeExecutiveSummary = true,
//                    MaxSections = 5,
//                    CreatedBy = "admin" // TODO: Get from auth
//                };

//                var response = await Http.PostAsJsonAsync("api/report/generate", request);
//                var result = await response.Content.ReadFromJsonAsync<GenerateReportResponse>();

//                if (result != null && result.Success && result.Report != null)
//                {
//                    currentReport = result.Report;
//                    reportTitle = currentReport.Title;

//                    // Initialize section states
//                    foreach (var section in currentReport.Sections ?? new List<ReportSection>())
//                    {
//                        InitializeSectionState(section.SectionID);
//                    }

//                    // Add AI message
//                    AddAssistantMessage($"I've created your report '{currentReport.Title}' with {currentReport.SectionCount} sections. {result.Explanation}");

//                    // Load data for all sections
//                    await LoadAllSectionData();

//                    // Update URL
//                    NavigationManager.NavigateTo($"/report/builder/{currentReport.ReportID}", false);
//                }
//                else
//                {
//                    await ShowError(result?.ErrorMessage ?? "Failed to generate report");
//                }
//            }
//            catch (Exception ex)
//            {
//                await ShowError($"Error generating report: {ex.Message}");
//            }
//            finally
//            {
//                isGenerating = false;
//                StateHasChanged();
//            }
//        }

//        private void UseExamplePrompt(string prompt)
//        {
//            generationPrompt = prompt;
//            StateHasChanged();
//        }

//        private async Task HandleGenerationKeyDown(KeyboardEventArgs e)
//        {
//            if (e.Key == "Enter" && e.CtrlKey)
//            {
//                await GenerateReport();
//            }
//        }

//        #endregion

//        #region Section Management

//        private async Task AddManualSection()
//        {
//            if (currentReport == null) return;

//            var section = new ReportSection
//            {
//                ReportID = currentReport.ReportID,
//                Title = "New Section",
//                SectionType = ReportSectionTypeEnum.DataTable,
//                DisplayOrder = currentReport.Sections?.Count ?? 0
//            };

//            try
//            {
//                var request = new CreateSectionRequest
//                {
//                    ReportID = currentReport.ReportID,
//                    Title = section.Title,
//                    SectionType = section.SectionType,
//                    DisplayOrder = section.DisplayOrder
//                };

//                var response = await Http.PostAsJsonAsync($"api/report/{currentReport.ReportID}/sections", request);
//                var createdSection = await response.Content.ReadFromJsonAsync<ReportSection>();

//                if (createdSection != null)
//                {
//                    currentReport.Sections ??= new List<ReportSection>();
//                    ((List<ReportSection>)currentReport.Sections).Add(createdSection);
//                    InitializeSectionState(createdSection.SectionID);
//                    StateHasChanged();
//                }
//            }
//            catch (Exception ex)
//            {
//                await ShowError($"Failed to add section: {ex.Message}");
//            }
//        }

//        private async Task ShowAddSectionWithAI()
//        {
//            if (!showAIAssistant)
//            {
//                showAIAssistant = true;
//            }

//            chatInput = "Add a section showing ";
//            StateHasChanged();

//            await Task.Delay(100);
//            await JSRuntime.InvokeVoidAsync("focusElement", ".chat-input");
//        }

//        private void ToggleSectionExpand(ReportSection section)
//        {
//            section.IsExpanded = !section.IsExpanded;
//            StateHasChanged();
//        }

//        private async Task MoveSection(ReportSection section, int direction)
//        {
//            if (currentReport?.Sections == null) return;

//            var sections = currentReport.Sections.OrderBy(s => s.DisplayOrder).ToList();
//            var currentIndex = sections.FindIndex(s => s.SectionID == section.SectionID);
//            var newIndex = currentIndex + direction;

//            if (newIndex < 0 || newIndex >= sections.Count) return;

//            // Swap display orders
//            var otherSection = sections[newIndex];
//            var tempOrder = section.DisplayOrder;
//            section.DisplayOrder = otherSection.DisplayOrder;
//            otherSection.DisplayOrder = tempOrder;

//            // Save to server
//            try
//            {
//                var request = new ReorderSectionsRequest
//                {
//                    ReportID = currentReport.ReportID,
//                    SectionOrder = currentReport.Sections.OrderBy(s => s.DisplayOrder).Select(s => s.SectionID).ToList()
//                };

//                await Http.PostAsJsonAsync($"api/report/{currentReport.ReportID}/sections/reorder", request);
//                StateHasChanged();
//            }
//            catch (Exception ex)
//            {
//                await ShowError($"Failed to reorder sections: {ex.Message}");
//            }
//        }

//        private async Task DeleteSection(ReportSection section)
//        {
//            if (currentReport?.Sections == null) return;

//            // TODO: Add confirmation dialog

//            try
//            {
//                var response = await Http.DeleteAsync($"api/report/{currentReport.ReportID}/sections/{section.SectionID}");

//                if (response.IsSuccessStatusCode)
//                {
//                    ((List<ReportSection>)currentReport.Sections).Remove(section);
//                    sectionData.Remove(section.SectionID);
//                    sectionLoading.Remove(section.SectionID);
//                    sectionErrors.Remove(section.SectionID);
//                    StateHasChanged();
//                }
//            }
//            catch (Exception ex)
//            {
//                await ShowError($"Failed to delete section: {ex.Message}");
//            }
//        }

//        private async Task ToggleChartMode(ReportSection section)
//        {
//            section.IsDisplayedAsChart = !section.IsDisplayedAsChart;

//            try
//            {
//                await Http.PatchAsync(
//                    $"api/report/{currentReport.ReportID}/sections/{section.SectionID}/chart-mode?displayAsChart={section.IsDisplayedAsChart}&chartType=bar",
//                    null);
//                StateHasChanged();
//            }
//            catch (Exception ex)
//            {
//                section.IsDisplayedAsChart = !section.IsDisplayedAsChart; // Rollback
//                await ShowError($"Failed to toggle chart mode: {ex.Message}");
//            }
//        }

//        private async Task RegenerateSummary(ReportSection section)
//        {
//            if (currentReport == null) return;

//            sectionLoading[section.SectionID] = true;
//            StateHasChanged();

//            try
//            {
//                var response = await Http.PostAsync($"api/report/{currentReport.ReportID}/regenerate-summary", null);
//                var updatedReport = await response.Content.ReadFromJsonAsync<DynamicDashboardCommon.Models.Report>();

//                if (updatedReport != null)
//                {
//                    // Update the section content
//                    var updatedSection = updatedReport.Sections?.FirstOrDefault(s => s.SectionID == section.SectionID);
//                    if (updatedSection != null)
//                    {
//                        section.TextContent = updatedSection.TextContent;
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                await ShowError($"Failed to regenerate summary: {ex.Message}");
//            }
//            finally
//            {
//                sectionLoading[section.SectionID] = false;
//                StateHasChanged();
//            }
//        }

//        #endregion

//        #region SQL Editor

//        private void ShowEditSqlModal(ReportSection section)
//        {
//            editingSection = section;
//            editingSql = section.QueryText ?? "";
//            testQueryResult = "";
//            testQuerySuccess = false;
//            showSqlModal = true;
//            StateHasChanged();
//        }

//        private void CloseSqlModal()
//        {
//            showSqlModal = false;
//            editingSection = null;
//            editingSql = "";
//            StateHasChanged();
//        }

//        private async Task TestSqlQuery()
//        {
//            if (string.IsNullOrWhiteSpace(editingSql)) return;

//            isTestingQuery = true;
//            testQueryResult = "";
//            StateHasChanged();

//            try
//            {
//                var request = new { Sql = editingSql, DatabaseId = selectedDatabaseId };
//                var response = await Http.PostAsJsonAsync("api/report/test-query", request);
//                var result = await response.Content.ReadFromJsonAsync<ExecuteSectionQueryResponse>();

//                if (result != null && result.Success)
//                {
//                    testQuerySuccess = true;
//                    testQueryResult = $"Query valid! Returns {result.TotalCount} rows in {result.ExecutionTimeMs}ms";
//                }
//                else
//                {
//                    testQuerySuccess = false;
//                    testQueryResult = result?.ErrorMessage ?? "Query validation failed";
//                }
//            }
//            catch (Exception ex)
//            {
//                testQuerySuccess = false;
//                testQueryResult = ex.Message;
//            }
//            finally
//            {
//                isTestingQuery = false;
//                StateHasChanged();
//            }
//        }

//        private async Task SaveSqlChanges()
//        {
//            if (editingSection == null || string.IsNullOrWhiteSpace(editingSql)) return;

//            try
//            {
//                var request = new UpdateSectionRequest
//                {
//                    SectionID = editingSection.SectionID,
//                    QueryText = editingSql
//                };

//                var response = await Http.PutAsJsonAsync(
//                    $"api/report/{currentReport.ReportID}/sections/{editingSection.SectionID}", 
//                    request);

//                if (response.IsSuccessStatusCode)
//                {
//                    editingSection.QueryText = editingSql;
//                    CloseSqlModal();

//                    // Reload section data
//                    await LoadSectionData(editingSection);
//                }
//            }
//            catch (Exception ex)
//            {
//                await ShowError($"Failed to save SQL: {ex.Message}");
//            }
//        }

//        #endregion

//        #region Column Configuration

//        private void ShowColumnConfigModal(ReportSection section)
//        {
//            editingSection = section;
//            editingColumnConfig = section.GetColumnConfiguration();

//            // If no columns, try to get from data
//            if (!editingColumnConfig.Columns.Any() && sectionData.ContainsKey(section.SectionID))
//            {
//                var data = sectionData[section.SectionID];
//                if (data.Any())
//                {
//                    editingColumnConfig.Columns = data[0].Keys.Select((key, index) => new ReportColumnDefinition
//                    {
//                        ColumnName = key,
//                        DisplayName = FormatColumnDisplayName(key),
//                        IsVisible = true,
//                        DisplayOrder = index,
//                        DataType = ColumnDataTypeEnum.Text,
//                        Alignment = ColumnAlignmentEnum.Left
//                    }).ToList();
//                }
//            }

//            showColumnModal = true;
//            StateHasChanged();
//        }

//        private void CloseColumnModal()
//        {
//            showColumnModal = false;
//            editingSection = null;
//            editingColumnConfig = null;
//            StateHasChanged();
//        }

//        private async Task SaveColumnConfig()
//        {
//            if (editingSection == null || editingColumnConfig == null) return;

//            try
//            {
//                var configJson = JsonSerializer.Serialize(editingColumnConfig, new JsonSerializerOptions
//                {
//                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
//                });

//                var request = new UpdateSectionRequest
//                {
//                    SectionID = editingSection.SectionID,
//                    ColumnConfiguration = configJson
//                };

//                var response = await Http.PutAsJsonAsync(
//                    $"api/report/{currentReport.ReportID}/sections/{editingSection.SectionID}",
//                    request);

//                if (response.IsSuccessStatusCode)
//                {
//                    editingSection.ColumnConfiguration = configJson;
//                    CloseColumnModal();
//                    StateHasChanged();
//                }
//            }
//            catch (Exception ex)
//            {
//                await ShowError($"Failed to save column configuration: {ex.Message}");
//            }
//        }

//        #endregion

//        #region Data Table Operations

//        private List<ReportColumnDefinition> GetVisibleColumns(ReportSection section)
//        {
//            var config = section.GetColumnConfiguration();

//            if (config.Columns.Any())
//            {
//                return config.Columns.Where(c => c.IsVisible).OrderBy(c => c.DisplayOrder).ToList();
//            }

//            // Fallback: generate from data
//            if (sectionData.ContainsKey(section.SectionID) && sectionData[section.SectionID].Any())
//            {
//                return sectionData[section.SectionID][0].Keys.Select((key, index) => new ReportColumnDefinition
//                {
//                    ColumnName = key,
//                    DisplayName = FormatColumnDisplayName(key),
//                    IsVisible = true,
//                    DisplayOrder = index
//                }).ToList();
//            }

//            return new List<ReportColumnDefinition>();
//        }

//        private List<Dictionary<string, object>> GetFilteredData(ReportSection section)
//        {
//            if (!sectionData.ContainsKey(section.SectionID))
//                return new List<Dictionary<string, object>>();

//            var data = sectionData[section.SectionID].AsEnumerable();

//            // Apply search filter
//            var searchTerm = sectionSearchTerms.GetValueOrDefault(section.SectionID, "")?.ToLower();
//            if (!string.IsNullOrEmpty(searchTerm))
//            {
//                data = data.Where(row => row.Values.Any(v => v?.ToString()?.ToLower()?.Contains(searchTerm) == true));
//            }

//            // Apply column filters
//            var visibleColumns = GetVisibleColumns(section);
//            foreach (var column in visibleColumns)
//            {
//                var filterKey = $"{section.SectionID}_{column.ColumnName}";
//                var filterValue = columnFilters.GetValueOrDefault(filterKey, "")?.ToLower();

//                if (!string.IsNullOrEmpty(filterValue))
//                {
//                    data = data.Where(row => 
//                        row.ContainsKey(column.ColumnName) && 
//                        row[column.ColumnName]?.ToString()?.ToLower()?.Contains(filterValue) == true);
//                }
//            }

//            // Apply sorting
//            var sortColumn = sectionSortColumn.GetValueOrDefault(section.SectionID);
//            if (!string.IsNullOrEmpty(sortColumn))
//            {
//                var ascending = sectionSortAscending.GetValueOrDefault(section.SectionID, true);
//                data = ascending
//                    ? data.OrderBy(row => row.GetValueOrDefault(sortColumn))
//                    : data.OrderByDescending(row => row.GetValueOrDefault(sortColumn));
//            }

//            return data.ToList();
//        }

//        private List<Dictionary<string, object>> GetPagedData(ReportSection section)
//        {
//            var filtered = GetFilteredData(section);
//            var page = sectionCurrentPage.GetValueOrDefault(section.SectionID, 1);
//            var pageSize = sectionPageSize.GetValueOrDefault(section.SectionID, 25);

//            return filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
//        }

//        private void FilterSectionData(ReportSection section)
//        {
//            sectionCurrentPage[section.SectionID] = 1; // Reset to first page
//            StateHasChanged();
//        }

//        private void SortByColumn(ReportSection section, string columnName)
//        {
//            if (sectionSortColumn.GetValueOrDefault(section.SectionID) == columnName)
//            {
//                sectionSortAscending[section.SectionID] = !sectionSortAscending.GetValueOrDefault(section.SectionID, true);
//            }
//            else
//            {
//                sectionSortColumn[section.SectionID] = columnName;
//                sectionSortAscending[section.SectionID] = true;
//            }
//            StateHasChanged();
//        }

//        private int GetCurrentPage(ReportSection section) => sectionCurrentPage.GetValueOrDefault(section.SectionID, 1);
//        private int GetPageSize(ReportSection section) => sectionPageSize.GetValueOrDefault(section.SectionID, 25);
//        private int GetTotalPages(ReportSection section)
//        {
//            var total = GetFilteredData(section).Count;
//            var pageSize = GetPageSize(section);
//            return (int)Math.Ceiling((double)total / pageSize);
//        }
//        private int GetPageStart(ReportSection section) => ((GetCurrentPage(section) - 1) * GetPageSize(section)) + 1;
//        private int GetPageEnd(ReportSection section) => Math.Min(GetCurrentPage(section) * GetPageSize(section), GetFilteredData(section).Count);

//        private void GoToPage(ReportSection section, int page)
//        {
//            var totalPages = GetTotalPages(section);
//            sectionCurrentPage[section.SectionID] = Math.Max(1, Math.Min(page, totalPages));
//            StateHasChanged();
//        }

//        private void ChangePageSize(ReportSection section, ChangeEventArgs e)
//        {
//            if (int.TryParse(e.Value?.ToString(), out var pageSize))
//            {
//                sectionPageSize[section.SectionID] = pageSize;
//                sectionCurrentPage[section.SectionID] = 1;
//                StateHasChanged();
//            }
//        }

//        private void ToggleExportDropdown(int sectionId)
//        {
//            showExportDropdown = showExportDropdown == sectionId ? 0 : sectionId;
//            StateHasChanged();
//        }

//        #endregion

//        #region Export Operations

//        private void ShowExportModal()
//        {
//            showExportModal = true;
//            StateHasChanged();
//        }

//        private void CloseExportModal()
//        {
//            showExportModal = false;
//            StateHasChanged();
//        }

//        private async Task ExportReport()
//        {
//            if (currentReport == null) return;

//            try
//            {
//                // TODO: Implement actual export
//                await ShowSuccess($"Report exported as {selectedExportFormat.ToUpper()}");
//                CloseExportModal();
//            }
//            catch (Exception ex)
//            {
//                await ShowError($"Export failed: {ex.Message}");
//            }
//        }

//        private async Task ExportSectionData(ReportSection section, string format)
//        {
//            showExportDropdown = 0;

//            try
//            {
//                // TODO: Implement section export
//                await ShowSuccess($"Section exported as {format.ToUpper()}");
//            }
//            catch (Exception ex)
//            {
//                await ShowError($"Export failed: {ex.Message}");
//            }
//        }

//        #endregion

//        #region AI Assistant

//        private void ToggleAIAssistant()
//        {
//            showAIAssistant = !showAIAssistant;
//            StateHasChanged();
//        }

//        private async Task SendChatMessage()
//        {
//            if (string.IsNullOrWhiteSpace(chatInput)) return;

//            var userMessage = chatInput.Trim();
//            chatInput = "";

//            // Add user message
//            chatMessages.Add(new AIChatMessage
//            {
//                Role = "user",
//                Content = userMessage,
//                Timestamp = DateTime.Now
//            });

//            isAIThinking = true;
//            StateHasChanged();

//            await ScrollChatToBottom();

//            try
//            {
//                var request = new AIChatRequest
//                {
//                    ReportID = currentReport?.ReportID,
//                    DatabaseID = selectedDatabaseId,
//                    Message = userMessage,
//                    ConversationHistory = chatMessages
//                };

//                var response = await Http.PostAsJsonAsync("api/report/chat", request);
//                var result = await response.Content.ReadFromJsonAsync<AIChatResponse>();

//                if (result != null)
//                {
//                    // Add assistant message
//                    chatMessages.Add(new AIChatMessage
//                    {
//                        Role = "assistant",
//                        Content = result.Message,
//                        Timestamp = DateTime.Now,
//                        Action = result.Action
//                    });

//                    // Handle actions
//                    if (result.Action == "report_generated" && result.UpdatedReport != null)
//                    {
//                        currentReport = result.UpdatedReport;
//                        reportTitle = currentReport.Title;

//                        foreach (var section in currentReport.Sections ?? new List<ReportSection>())
//                        {
//                            InitializeSectionState(section.SectionID);
//                        }

//                        await LoadAllSectionData();
//                        NavigationManager.NavigateTo($"/report/builder/{currentReport.ReportID}", false);
//                    }
//                    else if (result.Action == "section_added" && result.NewSection != null)
//                    {
//                        currentReport.Sections ??= new List<ReportSection>();
//                        ((List<ReportSection>)currentReport.Sections).Add(result.NewSection);
//                        InitializeSectionState(result.NewSection.SectionID);
//                        await LoadSectionData(result.NewSection);
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                chatMessages.Add(new AIChatMessage
//                {
//                    Role = "assistant",
//                    Content = $"Sorry, I encountered an error: {ex.Message}",
//                    Timestamp = DateTime.Now
//                });
//            }
//            finally
//            {
//                isAIThinking = false;
//                StateHasChanged();
//                await ScrollChatToBottom();
//            }
//        }

//        private async Task SendQuickMessage(string message)
//        {
//            chatInput = message;
//            await SendChatMessage();
//        }

//        private async Task HandleChatKeyDown(KeyboardEventArgs e)
//        {
//            if (e.Key == "Enter" && !e.ShiftKey)
//            {
//                await SendChatMessage();
//            }
//        }

//        private void AddAssistantMessage(string message)
//        {
//            chatMessages.Add(new AIChatMessage
//            {
//                Role = "assistant",
//                Content = message,
//                Timestamp = DateTime.Now
//            });
//        }

//        private async Task ScrollChatToBottom()
//        {
//            await Task.Delay(50);
//            await JSRuntime.InvokeVoidAsync("scrollToBottom", chatHistoryRef);
//        }

//        #endregion

//        #region Data Explanation

//        private async Task ExplainSectionData(ReportSection section)
//        {
//            if (!sectionData.ContainsKey(section.SectionID)) return;

//            showExplanationModal = true;
//            isExplaining = true;
//            currentExplanation = null;
//            StateHasChanged();

//            try
//            {
//                var request = new ExplainDataRequest
//                {
//                    SectionID = section.SectionID,
//                    Data = sectionData[section.SectionID].Take(100).ToList(),
//                    Context = section.QueryIntent ?? section.Title
//                };

//                var response = await Http.PostAsJsonAsync(
//                    $"api/report/{currentReport.ReportID}/sections/{section.SectionID}/explain",
//                    request);

//                currentExplanation = await response.Content.ReadFromJsonAsync<ExplainDataResponse>();
//            }
//            catch (Exception ex)
//            {
//                currentExplanation = new ExplainDataResponse
//                {
//                    Success = false,
//                    ErrorMessage = ex.Message
//                };
//            }
//            finally
//            {
//                isExplaining = false;
//                StateHasChanged();
//            }
//        }

//        private void CloseExplanationModal()
//        {
//            showExplanationModal = false;
//            currentExplanation = null;
//            StateHasChanged();
//        }

//        #endregion

//        #region Save Operations

//        private async Task SaveReport()
//        {
//            if (currentReport == null) return;

//            isSaving = true;
//            StateHasChanged();

//            try
//            {
//                var request = new UpdateReportRequest
//                {
//                    ReportID = currentReport.ReportID,
//                    Title = reportTitle,
//                    Description = currentReport.Description,
//                    Status = currentReport.Status,
//                    ModifiedBy = "admin" // TODO: Get from auth
//                };

//                var response = await Http.PutAsJsonAsync($"api/report/{currentReport.ReportID}", request);

//                if (response.IsSuccessStatusCode)
//                {
//                    currentReport.Title = reportTitle;
//                    await ShowSuccess("Report saved successfully!");
//                }
//                else
//                {
//                    await ShowError("Failed to save report");
//                }
//            }
//            catch (Exception ex)
//            {
//                await ShowError($"Error saving report: {ex.Message}");
//            }
//            finally
//            {
//                isSaving = false;
//                StateHasChanged();
//            }
//        }

//        #endregion

//        #region Helper Methods

//        private string GetStatusClass()
//        {
//            return currentReport?.Status switch
//            {
//                ReportStatusEnum.Draft => "status-draft",
//                ReportStatusEnum.Published => "status-published",
//                ReportStatusEnum.Archived => "status-archived",
//                _ => ""
//            };
//        }

//        private string GetSectionIconClass(ReportSectionTypeEnum type)
//        {
//            return type switch
//            {
//                ReportSectionTypeEnum.ExecutiveSummary => "icon-summary",
//                ReportSectionTypeEnum.DataTable => "icon-table",
//                ReportSectionTypeEnum.Chart => "icon-chart",
//                ReportSectionTypeEnum.TextBlock => "icon-text",
//                ReportSectionTypeEnum.KPICards => "icon-kpi",
//                _ => ""
//            };
//        }

//        private string GetColumnAlignmentClass(ReportColumnDefinition column)
//        {
//            return column.Alignment switch
//            {
//                ColumnAlignmentEnum.Right => "text-end",
//                ColumnAlignmentEnum.Center => "text-center",
//                _ => "text-start"
//            };
//        }

//        private string GetCellClass(ReportColumnDefinition column, object value)
//        {
//            // Apply conditional formatting
//            foreach (var rule in column.ConditionalFormats)
//            {
//                if (EvaluateCondition(rule, value))
//                {
//                    return $"formatted-cell";
//                }
//            }

//            return column.DataType switch
//            {
//                ColumnDataTypeEnum.Number => "cell-number",
//                ColumnDataTypeEnum.Currency => "cell-currency",
//                ColumnDataTypeEnum.Percentage => "cell-percentage",
//                _ => ""
//            };
//        }

//        private bool EvaluateCondition(ConditionalFormatRule rule, object value)
//        {
//            if (value == null) return rule.Operator == ConditionalOperatorEnum.IsEmpty;

//            try
//            {
//                var numValue = Convert.ToDouble(value);
//                var ruleValue = double.TryParse(rule.Value, out var rv) ? rv : 0;

//                return rule.Operator switch
//                {
//                    ConditionalOperatorEnum.GreaterThan => numValue > ruleValue,
//                    ConditionalOperatorEnum.LessThan => numValue < ruleValue,
//                    ConditionalOperatorEnum.Equals => Math.Abs(numValue - ruleValue) < 0.0001,
//                    _ => false
//                };
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        private string FormatCellValue(ReportColumnDefinition column, object value)
//        {
//            if (value == null) return "-";

//            return column.DataType switch
//            {
//                ColumnDataTypeEnum.Currency => $"${Convert.ToDecimal(value):N2}",
//                ColumnDataTypeEnum.Percentage => $"{Convert.ToDecimal(value):N1}%",
//                ColumnDataTypeEnum.Number => $"{Convert.ToDecimal(value):N0}",
//                ColumnDataTypeEnum.Date => value is DateTime dt ? dt.ToString("yyyy-MM-dd") : value.ToString(),
//                ColumnDataTypeEnum.DateTime => value is DateTime dtm ? dtm.ToString("yyyy-MM-dd HH:mm") : value.ToString(),
//                ColumnDataTypeEnum.Boolean => Convert.ToBoolean(value) ? "Yes" : "No",
//                _ => value.ToString()
//            };
//        }

//        private string FormatColumnDisplayName(string columnName)
//        {
//            var result = Regex.Replace(columnName, "([a-z])([A-Z])", "$1 $2");
//            result = result.Replace("_", " ");
//            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(result.ToLower());
//        }

//        private string FormatSummaryText(string text)
//        {
//            if (string.IsNullOrEmpty(text)) return "";

//            // Convert newlines to <br> and paragraphs
//            text = Regex.Replace(text, @"\n\n", "</p><p>");
//            text = Regex.Replace(text, @"\n", "<br/>");
//            text = $"<p>{text}</p>";

//            return text;
//        }

//        private string FormatChatMessage(string content)
//        {
//            if (string.IsNullOrEmpty(content)) return "";

//            // Convert markdown-style formatting
//            content = Regex.Replace(content, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
//            content = Regex.Replace(content, @"\*(.+?)\*", "<em>$1</em>");
//            content = Regex.Replace(content, @"•\s*(.+?)(?=\n|$)", "<li>$1</li>");
//            content = Regex.Replace(content, @"(<li>.*</li>)", "<ul>$1</ul>");
//            content = content.Replace("\n", "<br/>");

//            return content;
//        }

//        private ReportColumnConfiguration GenerateColumnConfig(List<ColumnMetadata> columns)
//        {
//            return new ReportColumnConfiguration
//            {
//                ShowSearch = true,
//                ShowColumnFilters = true,
//                ShowPagination = true,
//                ShowExport = true,
//                DefaultPageSize = 25,
//                Columns = columns.Select((c, index) => new ReportColumnDefinition
//                {
//                    ColumnName = c.Name,
//                    DisplayName = FormatColumnDisplayName(c.Name),
//                    IsVisible = true,
//                    DisplayOrder = index,
//                    DataType = MapDataType(c.DataType),
//                    Alignment = GetDefaultAlignment(c.DataType)
//                }).ToList()
//            };
//        }

//        private ColumnDataTypeEnum MapDataType(string dataType)
//        {
//            var lower = dataType?.ToLowerInvariant() ?? "";

//            if (lower.Contains("int") || lower.Contains("decimal") || lower.Contains("numeric") || lower.Contains("float"))
//                return ColumnDataTypeEnum.Number;
//            if (lower.Contains("money"))
//                return ColumnDataTypeEnum.Currency;
//            if (lower.Contains("date") && !lower.Contains("time"))
//                return ColumnDataTypeEnum.Date;
//            if (lower.Contains("datetime"))
//                return ColumnDataTypeEnum.DateTime;
//            if (lower.Contains("bit") || lower.Contains("bool"))
//                return ColumnDataTypeEnum.Boolean;

//            return ColumnDataTypeEnum.Text;
//        }

//        private ColumnAlignmentEnum GetDefaultAlignment(string dataType)
//        {
//            var type = MapDataType(dataType);
//            return type switch
//            {
//                ColumnDataTypeEnum.Number => ColumnAlignmentEnum.Right,
//                ColumnDataTypeEnum.Currency => ColumnAlignmentEnum.Right,
//                ColumnDataTypeEnum.Percentage => ColumnAlignmentEnum.Right,
//                _ => ColumnAlignmentEnum.Left
//            };
//        }

//        private async Task ShowError(string message)
//        {
//            await JSRuntime.InvokeVoidAsync("console.error", message);
//            // TODO: Integrate with toast service
//        }

//        private async Task ShowSuccess(string message)
//        {
//            await JSRuntime.InvokeVoidAsync("console.log", message);
//            // TODO: Integrate with toast service
//        }

//        #endregion
//    }
//}
