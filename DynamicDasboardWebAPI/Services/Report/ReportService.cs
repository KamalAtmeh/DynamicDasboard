//using DynamicDashboardCommon.Models;
//using DynamicDashboardCommon.Models;
//using DynamicDasboardWebAPI.Repositories;
//using DynamicDasboardWebAPI.Services;
//using DynamicDasboardWebAPI.Services.LLM;
//using Microsoft.Extensions.Configuration;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Text;
//using System.Text.Json;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;
//using DynamicDasboardWebAPI.Repositories;

//namespace DynamicDasboardWebAPI.Services.Report
//{
//    /// <summary>
//    /// Service implementation for Report business logic operations.
//    /// </summary>
//    public class ReportService : IReportService
//    {


//        private readonly IDatabaseService _databaseService;
//        private readonly DatabaseSchemaService _schemaService;
//        private readonly IConfiguration _configuration;
//        private readonly ReportRepository _repository;
//        private readonly LLMServiceFactory _llmServiceFactory;
//        private readonly ILLMService _llmService;
//        private readonly int _defaultPageSize;
//        private readonly int _maxSections;

//        /// <summary>
//        /// Initializes a new instance of the ReportService.
//        /// </summary>
//        public ReportService(
//                        LLMServiceFactory llmServiceFactory,
//                        ReportRepository reportrepository,
//            IDatabaseService databaseService,
//             DatabaseSchemaService schemaService,
//            IConfiguration configuration)
//        {
//            _repository = reportrepository ?? throw new ArgumentNullException(nameof(reportrepository));
//            //_llmService = llmServiceFactory?.CreateLlmService() ?? throw new ArgumentNullException(nameof(llmServiceFactory));
//            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
//            _schemaService = schemaService ?? throw new ArgumentNullException(nameof(schemaService));
//            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

//            _llmServiceFactory = llmServiceFactory ?? throw new ArgumentNullException(nameof(llmServiceFactory));
//            //_llmService = llmServiceFactory?.CreateLlmService() ?? throw new ArgumentNullException(nameof(llmServiceFactory));
//            // Create LLM service using factory
//            _llmService = _llmServiceFactory.CreateLlmService();

//            _defaultPageSize = _configuration.GetValue<int>("ReportBuilder:DefaultPageSize", 25);
//            _maxSections = _configuration.GetValue<int>("ReportBuilder:MaxSections", 10);
//        }

//        #region Report CRUD Operations

//        /// <inheritdoc />
//        public async Task<List<ReportListItemDto>> GetAllReportsAsync(int? databaseId = null, ReportStatusEnum? status = null, string createdBy = null)
//        {
//            return await _repository.GetAllReportsAsync(databaseId, status, createdBy);
//        }

//        /// <inheritdoc />
//        public async Task<DynamicDashboardCommon.Models.Report> GetReportByIdAsync(int reportId)
//        {
//            return await _repository.GetReportByIdAsync(reportId);
//        }

//        /// <inheritdoc />
//        public async Task<DynamicDashboardCommon.Models.Report> CreateReportAsync(CreateReportRequest request)
//        {
//            var report = new DynamicDashboardCommon.Models.Report
//            {
//                Title = request.Title,
//                Description = request.Description,
//                DatabaseID = request.DatabaseID,
//                ReportType = request.ReportType,
//                Status = ReportStatusEnum.Draft,
//                CreatedBy = request.CreatedBy
//            };

//            return await _repository.CreateReportAsync(report);
//        }

//        /// <inheritdoc />
//        public async Task<DynamicDashboardCommon.Models.Report> UpdateReportAsync(UpdateReportRequest request)
//        {
//            var report = await _repository.GetReportByIdAsync(request.ReportID);
//            if (report == null)
//                throw new InvalidOperationException($"Report with ID {request.ReportID} not found.");

//            report.Title = request.Title ?? report.Title;
//            report.Description = request.Description ?? report.Description;
//            report.Status = request.Status;
//            report.ExecutiveSummary = request.ExecutiveSummary ?? report.ExecutiveSummary;
//            report.Configuration = request.Configuration ?? report.Configuration;
//            report.LastModifiedBy = request.ModifiedBy;

//            return await _repository.UpdateReportAsync(report);
//        }

//        /// <inheritdoc />
//        public async Task<bool> DeleteReportAsync(int reportId)
//        {
//            return await _repository.DeleteReportAsync(reportId);
//        }

//        #endregion

//        #region Section Operations

//        /// <inheritdoc />
//        public async Task<ReportSection> AddSectionAsync(CreateSectionRequest request)
//        {
//            var section = new ReportSection
//            {
//                ReportID = request.ReportID,
//                Title = request.Title,
//                Description = request.Description,
//                SectionType = request.SectionType,
//                QueryText = request.QueryText,
//                QueryIntent = request.QueryIntent,
//                TextContent = request.TextContent,
//                DisplayOrder = request.DisplayOrder > 0 ? request.DisplayOrder : await _repository.GetNextSectionOrderAsync(request.ReportID)
//            };

//            return await _repository.CreateSectionAsync(section);
//        }

//        /// <inheritdoc />
//        public async Task<ReportSection> UpdateSectionAsync(UpdateSectionRequest request)
//        {
//            var section = await _repository.GetSectionByIdAsync(request.SectionID);
//            if (section == null)
//                throw new InvalidOperationException($"Section with ID {request.SectionID} not found.");

//            // Update only provided fields
//            if (!string.IsNullOrEmpty(request.Title))
//                section.Title = request.Title;

//            if (request.Description != null)
//                section.Description = request.Description;

//            if (!string.IsNullOrEmpty(request.QueryText))
//                section.QueryText = request.QueryText;

//            if (request.TextContent != null)
//                section.TextContent = request.TextContent;

//            if (!string.IsNullOrEmpty(request.ColumnConfiguration))
//                section.ColumnConfiguration = request.ColumnConfiguration;

//            if (request.IsVisible.HasValue)
//                section.IsVisible = request.IsVisible.Value;

//            if (request.IsDisplayedAsChart.HasValue)
//                section.IsDisplayedAsChart = request.IsDisplayedAsChart.Value;

//            if (!string.IsNullOrEmpty(request.ChartType))
//                section.ChartType = request.ChartType;

//            if (request.DisplayOrder.HasValue)
//                section.DisplayOrder = request.DisplayOrder.Value;

//            return await _repository.UpdateSectionAsync(section);
//        }

//        /// <inheritdoc />
//        public async Task<bool> DeleteSectionAsync(int sectionId)
//        {
//            return await _repository.DeleteSectionAsync(sectionId);
//        }

//        /// <inheritdoc />
//        public async Task<bool> ReorderSectionsAsync(ReorderSectionsRequest request)
//        {
//            return await _repository.ReorderSectionsAsync(request.ReportID, request.SectionOrder);
//        }

//        /// <inheritdoc />
//        public async Task<ReportSection> ToggleSectionVisibilityAsync(int sectionId, bool isVisible)
//        {
//            var section = await _repository.GetSectionByIdAsync(sectionId);
//            if (section == null)
//                throw new InvalidOperationException($"Section with ID {sectionId} not found.");

//            section.IsVisible = isVisible;
//            return await _repository.UpdateSectionAsync(section);
//        }

//        /// <inheritdoc />
//        public async Task<ReportSection> ToggleSectionChartModeAsync(int sectionId, bool displayAsChart, string chartType = "bar")
//        {
//            var section = await _repository.GetSectionByIdAsync(sectionId);
//            if (section == null)
//                throw new InvalidOperationException($"Section with ID {sectionId} not found.");

//            section.IsDisplayedAsChart = displayAsChart;
//            section.ChartType = displayAsChart ? chartType : null;

//            return await _repository.UpdateSectionAsync(section);
//        }

//        #endregion

//        #region AI Generation Operations

//        /// <inheritdoc />
//        public async Task<GenerateReportResponse> GenerateReportAsync(GenerateReportRequest request)
//        {
//            try
//            {
//                // Get database schema
//                var schemaText = await GetDatabaseSchemaAsync(request.DatabaseID);
//                if (string.IsNullOrEmpty(schemaText))
//                {
//                    return new GenerateReportResponse
//                    {
//                        Success = false,
//                        ErrorMessage = "Could not retrieve database schema."
//                    };
//                }

//                // Build AI prompt for report generation
//                var prompt = BuildReportGenerationPrompt(request.Prompt, schemaText, request.MaxSections);

//                // Call LLM to generate report structure
//                var llmResponse = await _llmService.GenerateSchemaAnalysisAsync(prompt);

//                // Parse LLM response
//                var reportStructure = ParseReportStructure(llmResponse);
//                if (reportStructure == null)
//                {
//                    return new GenerateReportResponse
//                    {
//                        Success = false,
//                        ErrorMessage = "Failed to parse AI response. Please try again."
//                    };
//                }

//                // Create report
//                var report = new DynamicDashboardCommon.Models.Report
//                {
//                    Title = !string.IsNullOrEmpty(request.Title) ? request.Title : reportStructure.Title,
//                    Description = reportStructure.Description,
//                    DatabaseID = request.DatabaseID,
//                    ReportType = DetermineReportType(request.Prompt),
//                    Status = ReportStatusEnum.Draft,
//                    GeneratedPrompt = request.Prompt,
//                    LLMProvider = _llmService.GetType().Name,
//                    CreatedBy = request.CreatedBy
//                };

//                // Create sections from AI response
//                var sections = new List<ReportSection>();
//                int order = 0;

//                // Add executive summary if requested
//                if (request.IncludeExecutiveSummary)
//                {
//                    var summarySection = new ReportSection
//                    {
//                        Title = "Executive Summary",
//                        Description = "AI-generated overview of the report data",
//                        SectionType = ReportSectionTypeEnum.ExecutiveSummary,
//                        DisplayOrder = order++,
//                        TextContent = "Generating summary... (will be populated after data is loaded)"
//                    };
//                    sections.Add(summarySection);
//                }

//                // Add data sections from AI
//                foreach (var sectionDef in reportStructure.Sections.Take(request.MaxSections))
//                {
//                    var section = new ReportSection
//                    {
//                        Title = sectionDef.Title,
//                        Description = sectionDef.Description,
//                        SectionType = ReportSectionTypeEnum.DataTable,
//                        QueryText = sectionDef.QueryText,
//                        QueryIntent = sectionDef.QueryIntent,
//                        DisplayOrder = order++,
//                        IsVisible = true,
//                        IsExpanded = true
//                    };

//                    // Auto-generate column configuration
//                    if (!string.IsNullOrEmpty(sectionDef.QueryText))
//                    {
//                        try
//                        {
//                            var columns = await _repository.GetQueryColumnsAsync(sectionDef.QueryText, request.DatabaseID);
//                            section.ColumnConfiguration = GenerateDefaultColumnConfig(columns);
//                        }
//                        catch
//                        {
//                            // Query might have issues, we'll let user fix it
//                        }
//                    }

//                    sections.Add(section);
//                }

//                report.Sections = sections;

//                // Save to database
//                var createdReport = await _repository.CreateReportAsync(report);

//                // Generate executive summary if sections have data
//                if (request.IncludeExecutiveSummary && createdReport.Sections.Any())
//                {
//                    await GenerateExecutiveSummaryAsync(createdReport);
//                }

//                return new GenerateReportResponse
//                {
//                    Success = true,
//                    Report = createdReport,
//                    Explanation = reportStructure.Explanation,
//                    Suggestions = reportStructure.Suggestions
//                };
//            }
//            catch (Exception ex)
//            {
//                return new GenerateReportResponse
//                {
//                    Success = false,
//                    ErrorMessage = $"Error generating report: {ex.Message}"
//                };
//            }
//        }

//        /// <inheritdoc />
//        public async Task<GenerateSectionResponse> GenerateSectionAsync(GenerateSectionRequest request)
//        {
//            try
//            {
//                // Get database schema
//                var schemaText = await GetDatabaseSchemaAsync(request.DatabaseID);
//                if (string.IsNullOrEmpty(schemaText))
//                {
//                    return new GenerateSectionResponse
//                    {
//                        Success = false,
//                        ErrorMessage = "Could not retrieve database schema."
//                    };
//                }

//                // Build prompt for single section generation
//                var prompt = BuildSectionGenerationPrompt(request.Prompt, schemaText);

//                // Call LLM
//                var llmResponse = await _llmService.GenerateSqlWithExplanationAsync(request.Prompt, schemaText);

//                if (llmResponse == null || !llmResponse.Success)
//                {
//                    return new GenerateSectionResponse
//                    {
//                        Success = false,
//                        ErrorMessage = llmResponse?.ErrorMessage ?? "Failed to generate section."
//                    };
//                }

//                // Get next display order
//                var displayOrder = await _repository.GetNextSectionOrderAsync(request.ReportID);

//                // Create section
//                var section = new ReportSection
//                {
//                    ReportID = request.ReportID,
//                    Title = GenerateTitleFromPrompt(request.Prompt), //TODO : Make the admin select titles
//                    Description = request.Prompt,
//                    SectionType = request.PreferredSectionType ?? ReportSectionTypeEnum.DataTable,
//                    QueryText = llmResponse.SqlQuery,
//                    QueryIntent = request.Prompt,
//                    DisplayOrder = displayOrder,
//                    IsVisible = true,
//                    IsExpanded = true
//                };

//                // Get column configuration
//                try
//                {
//                    var columns = await _repository.GetQueryColumnsAsync(llmResponse.SqlQuery, request.DatabaseID);
//                    section.ColumnConfiguration = GenerateDefaultColumnConfig(columns);
//                }
//                catch
//                {
//                    // Query might have issues
//                }

//                // Save section
//                var createdSection = await _repository.CreateSectionAsync(section);

//                return new GenerateSectionResponse
//                {
//                    Success = true,
//                    Section = createdSection,
//                    Explanation = llmResponse.BusinessExplanation
//                };
//            }
//            catch (Exception ex)
//            {
//                return new GenerateSectionResponse
//                {
//                    Success = false,
//                    ErrorMessage = $"Error generating section: {ex.Message}"
//                };
//            }
//        }

//        /// <inheritdoc />
//        public async Task<DynamicDashboardCommon.Models.Report> RegenerateExecutiveSummaryAsync(int reportId)
//        {
//            var report = await _repository.GetReportByIdAsync(reportId);
//            if (report == null)
//                throw new InvalidOperationException($"Report with ID {reportId} not found.");

//            await GenerateExecutiveSummaryAsync(report);

//            return await _repository.GetReportByIdAsync(reportId);
//        }

//        /// <inheritdoc />
//        public async Task<ExplainDataResponse> ExplainDataAsync(ExplainDataRequest request)
//        {
//            try
//            {
//                var section = await _repository.GetSectionByIdAsync(request.SectionID);
//                if (section == null)
//                {
//                    return new ExplainDataResponse
//                    {
//                        Success = false,
//                        ErrorMessage = "Section not found."
//                    };
//                }

//                // Build prompt for data explanation
//                var prompt = BuildDataExplanationPrompt(section.Title, section.QueryIntent, request.Data, request.Context);

//                // Get explanation from LLM
//                var explanation = await _llmService.GenerateResultExplanationAsync(
//                    request.Context ?? section.QueryIntent,
//                    section.QueryText,
//                    request.Data);

//                // Parse insights from explanation
//                var insights = ExtractInsights(explanation);

//                return new ExplainDataResponse
//                {
//                    Success = true,
//                    Explanation = explanation,
//                    KeyInsights = insights.KeyInsights,
//                    Trends = insights.Trends
//                };
//            }
//            catch (Exception ex)
//            {
//                return new ExplainDataResponse
//                {
//                    Success = false,
//                    ErrorMessage = $"Error explaining data: {ex.Message}"
//                };
//            }
//        }

//        /// <inheritdoc />
//        public async Task<AIChatResponse> ProcessChatMessageAsync(AIChatRequest request)
//        {
//            try
//            {
//                // Analyze intent from message
//                var intent = AnalyzeChatIntent(request.Message);

//                switch (intent)
//                {
//                    case ChatIntent.GenerateReport:
//                        var reportResponse = await GenerateReportAsync(new GenerateReportRequest
//                        {
//                            Prompt = request.Message,
//                            DatabaseID = request.DatabaseID,
//                            IncludeExecutiveSummary = true,
//                            MaxSections = 5
//                        });

//                        return new AIChatResponse
//                        {
//                            Success = reportResponse.Success,
//                            Message = reportResponse.Success
//                                ? $"I've created a report with {reportResponse.Report?.SectionCount} sections based on your request."
//                                : reportResponse.ErrorMessage,
//                            Action = "report_generated",
//                            UpdatedReport = reportResponse.Report
//                        };

//                    case ChatIntent.AddSection:
//                        if (!request.ReportID.HasValue)
//                        {
//                            return new AIChatResponse
//                            {
//                                Success = false,
//                                Message = "Please create or open a report first before adding sections."
//                            };
//                        }

//                        var sectionResponse = await GenerateSectionAsync(new GenerateSectionRequest
//                        {
//                            ReportID = request.ReportID.Value,
//                            Prompt = request.Message,
//                            DatabaseID = request.DatabaseID
//                        });

//                        return new AIChatResponse
//                        {
//                            Success = sectionResponse.Success,
//                            Message = sectionResponse.Success
//                                ? $"I've added a new section '{sectionResponse.Section?.Title}' to your report."
//                                : sectionResponse.ErrorMessage,
//                            Action = "section_added",
//                            NewSection = sectionResponse.Section
//                        };

//                    case ChatIntent.ExplainData:
//                        // For data explanation, we need the section ID from context
//                        return new AIChatResponse
//                        {
//                            Success = true,
//                            Message = "To explain data, please select a specific section first.",
//                            Action = "need_section_context"
//                        };

//                    default:
//                        // General chat - provide helpful response
//                        return new AIChatResponse
//                        {
//                            Success = true,
//                            Message = GetHelpfulResponse(request.Message),
//                            Action = "general_response"
//                        };
//                }
//            }
//            catch (Exception ex)
//            {
//                return new AIChatResponse
//                {
//                    Success = false,
//                    Message = $"Sorry, I encountered an error: {ex.Message}"
//                };
//            }
//        }

//        #endregion

//        #region Section Data Operations

//        /// <inheritdoc />
//        public async Task<ExecuteSectionQueryResponse> ExecuteSectionQueryAsync(ExecuteSectionQueryRequest request)
//        {
//            var stopwatch = Stopwatch.StartNew();

//            try
//            {
//                var section = await _repository.GetSectionByIdAsync(request.SectionID);
//                if (section == null)
//                {
//                    return new ExecuteSectionQueryResponse
//                    {
//                        Success = false,
//                        ErrorMessage = "Section not found."
//                    };
//                }

//                if (string.IsNullOrEmpty(section.QueryText))
//                {
//                    return new ExecuteSectionQueryResponse
//                    {
//                        Success = false,
//                        ErrorMessage = "Section has no query defined."
//                    };
//                }

//                // Get report for database ID
//                var report = await _repository.GetReportHeaderAsync(section.ReportID);
//                if (report == null)
//                {
//                    return new ExecuteSectionQueryResponse
//                    {
//                        Success = false,
//                        ErrorMessage = "Report not found."
//                    };
//                }

//                // Execute with pagination
//                var (data, totalCount) = await _repository.ExecuteQueryWithPaginationAsync(
//                    section.QueryText,
//                    report.DatabaseID,
//                    request.Page,
//                    request.PageSize > 0 ? request.PageSize : _defaultPageSize,
//                    request.SortColumn,
//                    request.SortDirection);

//                // Get column metadata
//                var columns = await _repository.GetQueryColumnsAsync(section.QueryText, report.DatabaseID);

//                stopwatch.Stop();

//                return new ExecuteSectionQueryResponse
//                {
//                    Success = true,
//                    Data = data,
//                    TotalCount = totalCount,
//                    Columns = columns,
//                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds
//                };
//            }
//            catch (Exception ex)
//            {
//                stopwatch.Stop();
//                return new ExecuteSectionQueryResponse
//                {
//                    Success = false,
//                    ErrorMessage = $"Query execution failed: {ex.Message}",
//                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds
//                };
//            }
//        }

//        /// <inheritdoc />
//        public async Task<ExecuteSectionQueryResponse> ExecuteSectionQueryFullAsync(int sectionId)
//        {
//            var stopwatch = Stopwatch.StartNew();

//            try
//            {
//                var section = await _repository.GetSectionByIdAsync(sectionId);
//                if (section == null)
//                {
//                    return new ExecuteSectionQueryResponse
//                    {
//                        Success = false,
//                        ErrorMessage = "Section not found."
//                    };
//                }

//                var report = await _repository.GetReportHeaderAsync(section.ReportID);
//                if (report == null)
//                {
//                    return new ExecuteSectionQueryResponse
//                    {
//                        Success = false,
//                        ErrorMessage = "Report not found."
//                    };
//                }

//                var data = await _repository.ExecuteQueryAsync(section.QueryText, report.DatabaseID);
//                var columns = await _repository.GetQueryColumnsAsync(section.QueryText, report.DatabaseID);

//                stopwatch.Stop();

//                return new ExecuteSectionQueryResponse
//                {
//                    Success = true,
//                    Data = data,
//                    TotalCount = data.Count,
//                    Columns = columns,
//                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds
//                };
//            }
//            catch (Exception ex)
//            {
//                stopwatch.Stop();
//                return new ExecuteSectionQueryResponse
//                {
//                    Success = false,
//                    ErrorMessage = $"Query execution failed: {ex.Message}",
//                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds
//                };
//            }
//        }

//        /// <inheritdoc />
//        public async Task<ExecuteSectionQueryResponse> TestQueryAsync(string sql, int databaseId)
//        {
//            var stopwatch = Stopwatch.StartNew();

//            try
//            {
//                // Validate first
//                var (isValid, errorMessage) = await _repository.ValidateQueryAsync(sql, databaseId);
//                if (!isValid)
//                {
//                    return new ExecuteSectionQueryResponse
//                    {
//                        Success = false,
//                        ErrorMessage = errorMessage
//                    };
//                }

//                // Execute with limit for testing
//                var testSql = $"SELECT TOP 100 * FROM ({sql}) AS TestQuery";
//                var data = await _repository.ExecuteQueryAsync(testSql, databaseId);
//                var columns = await _repository.GetQueryColumnsAsync(sql, databaseId);

//                stopwatch.Stop();

//                return new ExecuteSectionQueryResponse
//                {
//                    Success = true,
//                    Data = data,
//                    TotalCount = data.Count,
//                    Columns = columns,
//                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds
//                };
//            }
//            catch (Exception ex)
//            {
//                stopwatch.Stop();
//                return new ExecuteSectionQueryResponse
//                {
//                    Success = false,
//                    ErrorMessage = $"Query test failed: {ex.Message}",
//                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds
//                };
//            }
//        }

//        /// <inheritdoc />
//        public async Task<List<ColumnMetadata>> GetSectionColumnsAsync(int sectionId)
//        {
//            var section = await _repository.GetSectionByIdAsync(sectionId);
//            if (section == null || string.IsNullOrEmpty(section.QueryText))
//                return new List<ColumnMetadata>();

//            var report = await _repository.GetReportHeaderAsync(section.ReportID);
//            if (report == null)
//                return new List<ColumnMetadata>();

//            return await _repository.GetQueryColumnsAsync(section.QueryText, report.DatabaseID);
//        }

//        #endregion

//        #region Export Operations

//        /// <inheritdoc />
//        public async Task<byte[]> ExportReportAsync(ExportReportRequest request)
//        {
//            var report = await _repository.GetReportByIdAsync(request.ReportID);
//            if (report == null)
//                throw new InvalidOperationException($"Report with ID {request.ReportID} not found.");

//            // TODO: Implement actual export logic based on format
//            // This would integrate with your existing export services

//            switch (request.Format)
//            {
//                case ReportExportFormat.PDF:
//                    return await ExportToPdfAsync(report, request);

//                case ReportExportFormat.Excel:
//                    return await ExportToExcelAsync(report, request);

//                case ReportExportFormat.CSV:
//                    return await ExportToCsvAsync(report, request);

//                default:
//                    throw new NotSupportedException($"Export format {request.Format} not supported.");
//            }
//        }

//        /// <inheritdoc />
//        public async Task<byte[]> ExportSectionDataAsync(int sectionId, ReportExportFormat format)
//        {
//            var response = await ExecuteSectionQueryFullAsync(sectionId);
//            if (!response.Success)
//                throw new InvalidOperationException(response.ErrorMessage);

//            // TODO: Implement section export
//            return Array.Empty<byte>();
//        }

//        #endregion

//        #region Private Helper Methods

//        private async Task<string> GetDatabaseSchemaAsync(int databaseId)
//        {
//            var schemaObj = await _schemaService.GetSchemaObject(databaseId);

//            if (schemaObj != null)
//            {
//                return _schemaService.BuildOptimizedSchemaString(schemaObj);
//            }

//            var database = await _databaseService.GetDatabaseByIdAsync(databaseId);
//            if (database == null)
//                return null;

//            schemaObj = await _schemaService.GenerateAndGetDatabaseSchemaFromConnectedDBAsync(databaseId, database);
//            return _schemaService.BuildOptimizedSchemaString(schemaObj);
//        }

//        private string BuildReportGenerationPrompt(string userPrompt, string schema, int maxSections)
//        {
//            return $@"
//You are an expert business analyst creating a data report.

//USER REQUEST: {userPrompt}

//DATABASE SCHEMA:
//{schema}

//Generate a report structure with {maxSections} sections. Respond ONLY with valid JSON in this exact format:
//{{
//  ""title"": ""Report Title"",
//  ""description"": ""Brief description of the report"",
//  ""explanation"": ""Explanation of report structure for the user"",
//  ""sections"": [
//    {{
//      ""title"": ""Section Title"",
//      ""description"": ""What this section shows"",
//      ""queryText"": ""SELECT column1, column2 FROM table WHERE condition GROUP BY column1"",
//      ""queryIntent"": ""Natural language description of the query""
//    }}
//  ],
//  ""suggestions"": [
//    {{
//      ""title"": ""Additional Section Suggestion"",
//      ""description"": ""Why this would be valuable"",
//      ""queryIntent"": ""What data it would show""
//    }}
//  ]
//}}

//RULES:
//1. Generate valid SQL queries using ONLY tables and columns from the schema
//2. Each section should provide valuable business insights
//3. Include aggregations (SUM, COUNT, AVG) where appropriate
//4. Order results meaningfully (by value DESC, date, etc.)
//5. Keep queries simple but informative
//6. Section titles should be business-friendly
//";
//        }

//        private string BuildSectionGenerationPrompt(string userPrompt, string schema)
//        {
//            return $@"
//Generate a SQL query for this request: {userPrompt}

//DATABASE SCHEMA:
//{schema}

//Return a query that provides valuable business insights.
//";
//        }

//        private string BuildDataExplanationPrompt(string sectionTitle, string queryIntent, List<Dictionary<string, object>> data, string context)
//        {
//            var dataSummary = SummarizeData(data);

//            return $@"
//Explain this data in business terms:

//SECTION: {sectionTitle}
//PURPOSE: {queryIntent}
//CONTEXT: {context ?? "General analysis"}

//DATA SUMMARY:
//{dataSummary}

//Provide:
//1. A clear business explanation of what this data shows
//2. Key insights (3-5 bullet points)
//3. Any notable trends or patterns
//";
//        }

//        private string SummarizeData(List<Dictionary<string, object>> data)
//        {
//            if (data == null || !data.Any())
//                return "No data available.";

//            var sb = new StringBuilder();
//            sb.AppendLine($"Total rows: {data.Count}");

//            if (data.Any())
//            {
//                sb.AppendLine($"Columns: {string.Join(", ", data[0].Keys)}");

//                // Sample first few rows
//                sb.AppendLine("Sample data:");
//                foreach (var row in data.Take(5))
//                {
//                    sb.AppendLine(string.Join(" | ", row.Values.Select(v => v?.ToString() ?? "NULL")));
//                }
//            }

//            return sb.ToString();
//        }

//        private ReportStructure ParseReportStructure(string llmResponse)
//        {
//            try
//            {
//                // Extract JSON from response
//                var jsonMatch = Regex.Match(llmResponse, @"\{[\s\S]*\}", RegexOptions.Multiline);
//                if (!jsonMatch.Success)
//                    return null;

//                var json = jsonMatch.Value;
//                return JsonSerializer.Deserialize<ReportStructure>(json, new JsonSerializerOptions
//                {
//                    PropertyNameCaseInsensitive = true
//                });
//            }
//            catch
//            {
//                return null;
//            }
//        }

//        private ReportTypeEnum DetermineReportType(string prompt)
//        {
//            var lowerPrompt = prompt.ToLowerInvariant();

//            if (lowerPrompt.Contains("sales") || lowerPrompt.Contains("revenue"))
//                return ReportTypeEnum.Sales;
//            if (lowerPrompt.Contains("finance") || lowerPrompt.Contains("financial") || lowerPrompt.Contains("budget"))
//                return ReportTypeEnum.Financial;
//            if (lowerPrompt.Contains("hr") || lowerPrompt.Contains("employee") || lowerPrompt.Contains("human resource"))
//                return ReportTypeEnum.HR;
//            if (lowerPrompt.Contains("operation") || lowerPrompt.Contains("operational"))
//                return ReportTypeEnum.Operations;
//            if (lowerPrompt.Contains("marketing") || lowerPrompt.Contains("campaign"))
//                return ReportTypeEnum.Marketing;
//            if (lowerPrompt.Contains("inventory") || lowerPrompt.Contains("stock"))
//                return ReportTypeEnum.Inventory;
//            if (lowerPrompt.Contains("customer"))
//                return ReportTypeEnum.Customer;

//            return ReportTypeEnum.Custom;
//        }

//        private string GenerateDefaultColumnConfig(List<ColumnMetadata> columns)
//        {
//            var config = new ReportColumnConfiguration
//            {
//                ShowSearch = true,
//                ShowColumnFilters = true,
//                ShowPagination = true,
//                ShowExport = true,
//                DefaultPageSize = _defaultPageSize,
//                Columns = columns.Select((c, index) => new ReportColumnDefinition
//                {
//                    ColumnName = c.Name,
//                    DisplayName = FormatColumnName(c.Name),
//                    IsVisible = true,
//                    DisplayOrder = index,
//                    DataType = MapDataType(c.DataType),
//                    Alignment = GetDefaultAlignment(c.DataType),
//                    IsSortable = true,
//                    IsFilterable = true
//                }).ToList()
//            };

//            return JsonSerializer.Serialize(config, new JsonSerializerOptions
//            {
//                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
//            });
//        }

//        private string FormatColumnName(string columnName)
//        {
//            // Convert PascalCase or snake_case to readable format
//            var result = Regex.Replace(columnName, "([a-z])([A-Z])", "$1 $2");
//            result = result.Replace("_", " ");
//            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(result.ToLower());
//        }

//        private ColumnDataTypeEnum MapDataType(string dataType)
//        {
//            var lower = dataType?.ToLowerInvariant() ?? "";

//            if (lower.Contains("int") || lower.Contains("decimal") || lower.Contains("numeric") || lower.Contains("float") || lower.Contains("real"))
//                return ColumnDataTypeEnum.Number;
//            if (lower.Contains("money"))
//                return ColumnDataTypeEnum.Currency;
//            if (lower.Contains("date") && !lower.Contains("time"))
//                return ColumnDataTypeEnum.Date;
//            if (lower.Contains("datetime") || lower.Contains("timestamp"))
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

//        private string GenerateTitleFromPrompt(string prompt)
//        {
//            // Extract meaningful title from prompt
//            var words = prompt.Split(' ').Take(5);
//            var title = string.Join(" ", words);
//            return title.Length > 50 ? title.Substring(0, 47) + "..." : title;
//        }

//        private async Task GenerateExecutiveSummaryAsync(DynamicDashboardCommon.Models.Report report)
//        {
//            try
//            {
//                // Collect data from all sections
//                var allData = new List<string>();

//                foreach (var section in report.Sections.Where(s => s.SectionType == ReportSectionTypeEnum.DataTable))
//                {
//                    if (string.IsNullOrEmpty(section.QueryText))
//                        continue;

//                    try
//                    {
//                        var data = await _repository.ExecuteQueryAsync(section.QueryText, report.DatabaseID);
//                        if (data.Any())
//                        {
//                            allData.Add($"{section.Title}: {data.Count} records");
//                        }
//                    }
//                    catch
//                    {
//                        // Skip sections with query errors
//                    }
//                }

//                if (!allData.Any())
//                    return;

//                // Generate summary using LLM
//                var prompt = $@"
//Generate a brief executive summary for a report titled '{report.Title}'.
//The report contains the following sections:
//{string.Join("\n", allData)}

//Original request: {report.GeneratedPrompt}

//Write 2-3 paragraphs summarizing what insights this report provides.
//";

//                var summary = await _llmService.GenerateSchemaAnalysisAsync(prompt);

//                // Update executive summary section
//                var summarySection = report.Sections.FirstOrDefault(s => s.SectionType == ReportSectionTypeEnum.ExecutiveSummary);
//                if (summarySection != null)
//                {
//                    summarySection.TextContent = CleanSummaryText(summary);
//                    await _repository.UpdateSectionAsync(summarySection);
//                }

//                // Also update report's executive summary field
//                report.ExecutiveSummary = summarySection?.TextContent;
//                await _repository.UpdateReportAsync(report);
//            }
//            catch
//            {
//                // Executive summary generation is optional, don't fail the whole operation
//            }
//        }

//        private string CleanSummaryText(string text)
//        {
//            if (string.IsNullOrEmpty(text))
//                return text;

//            // Remove any JSON or code block formatting
//            text = Regex.Replace(text, @"```[\s\S]*?```", "");
//            text = Regex.Replace(text, @"\{[\s\S]*?\}", "");

//            return text.Trim();
//        }

//        private (List<string> KeyInsights, List<string> Trends) ExtractInsights(string explanation)
//        {
//            var insights = new List<string>();
//            var trends = new List<string>();

//            // Simple extraction - in production, you might use more sophisticated NLP
//            var lines = explanation.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l));

//            foreach (var line in lines)
//            {
//                var trimmed = line.Trim();
//                if (trimmed.StartsWith("-") || trimmed.StartsWith("•") || trimmed.StartsWith("*"))
//                {
//                    var content = trimmed.TrimStart('-', '•', '*', ' ');
//                    if (content.ToLower().Contains("trend") || content.ToLower().Contains("increase") || content.ToLower().Contains("decrease"))
//                        trends.Add(content);
//                    else
//                        insights.Add(content);
//                }
//            }

//            return (insights.Take(5).ToList(), trends.Take(3).ToList());
//        }

//        private ChatIntent AnalyzeChatIntent(string message)
//        {
//            var lower = message.ToLowerInvariant();

//            if (lower.Contains("create") && (lower.Contains("report") || lower.Contains("dashboard")))
//                return ChatIntent.GenerateReport;

//            if (lower.Contains("add") && (lower.Contains("section") || lower.Contains("table") || lower.Contains("chart")))
//                return ChatIntent.AddSection;

//            if (lower.Contains("explain") || lower.Contains("what does") || lower.Contains("tell me about"))
//                return ChatIntent.ExplainData;

//            return ChatIntent.General;
//        }

//        private string GetHelpfulResponse(string message)
//        {
//            return @"I can help you with:
//• **Create a report** - Just tell me what kind of report you need (e.g., 'Create a monthly sales report')
//• **Add sections** - Ask me to add specific data sections (e.g., 'Add a section showing top customers')
//• **Explain data** - Select a section and ask me to explain what the data means

//What would you like to do?";
//        }

//        // Export implementations (stubs - integrate with your existing export services)
//        private async Task<byte[]> ExportToPdfAsync(DynamicDashboardCommon.Models.Report report, ExportReportRequest request)
//        {
//            // TODO: Implement PDF export using your existing PDF service
//            await Task.CompletedTask;
//            throw new NotImplementedException("PDF export not yet implemented.");
//        }

//        private async Task<byte[]> ExportToExcelAsync(DynamicDashboardCommon.Models.Report report, ExportReportRequest request)
//        {
//            // TODO: Implement Excel export using EPPlus or your existing export service
//            await Task.CompletedTask;
//            throw new NotImplementedException("Excel export not yet implemented.");
//        }

//        private async Task<byte[]> ExportToCsvAsync(DynamicDashboardCommon.Models.Report report, ExportReportRequest request)
//        {
//            // TODO: Implement CSV export
//            await Task.CompletedTask;
//            throw new NotImplementedException("CSV export not yet implemented.");
//        }

//        #endregion

//        #region Private Classes

//        private enum ChatIntent
//        {
//            GenerateReport,
//            AddSection,
//            ExplainData,
//            General
//        }

//        private class ReportStructure
//        {
//            public string Title { get; set; }
//            public string Description { get; set; }
//            public string Explanation { get; set; }
//            public List<SectionDefinition> Sections { get; set; } = new List<SectionDefinition>();
//            public List<SectionSuggestion> Suggestions { get; set; } = new List<SectionSuggestion>();
//        }

//        private class SectionDefinition
//        {
//            public string Title { get; set; }
//            public string Description { get; set; }
//            public string QueryText { get; set; }
//            public string QueryIntent { get; set; }
//        }

//        #endregion
//    }
//}
