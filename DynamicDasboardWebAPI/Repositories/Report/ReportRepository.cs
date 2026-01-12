//using Dapper;
//using DynamicDasboardWebAPI.Utilities;
//using DynamicDashboardCommon.Models;
//using Microsoft.AspNetCore.Connections;
//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Linq;
//using System.Threading.Tasks;

//namespace DynamicDasboardWebAPI.Repositories
//{
//    /// <summary>
//    /// Repository implementation for Report data access operations.
//    /// </summary>
//    public class ReportRepository : BaseRepository, IReportRepository
//    {
//        /// <summary>
//        /// Initializes a new instance of the <see cref="ReportRepository"/> class.
//        /// </summary>
//        /// <param name="appDbConnection">The database connection.</param>
//        /// <param name="connectionFactory">The connection factory.</param>
//        public ReportRepository(
//            IDbConnection appDbConnection,
//            DbConnectionFactory connectionFactory)
//            : base(appDbConnection, connectionFactory)
//        {
//        }

//        #region Report CRUD Operations

//        /// <summary>
//        /// Gets all reports with optional filtering.
//        /// </summary>
//        /// <param name="databaseId">Optional database ID filter.</param>
//        /// <param name="status">Optional status filter.</param>
//        /// <param name="createdBy">Optional creator filter.</param>
//        /// <returns>List of reports.</returns>
//        public async Task<List<ReportListItemDto>> GetAllReportsAsync(
//            int? databaseId = null,
//            ReportStatusEnum? status = null,
//            string createdBy = null)
//        {
//            try
//            {
//                var parameters = new DynamicParameters();
//                var whereConditions = new List<string>();

//                if (databaseId.HasValue)
//                {
//                    whereConditions.Add("r.DatabaseID = @DatabaseID");
//                    parameters.Add("@DatabaseID", databaseId.Value);
//                }

//                if (status.HasValue)
//                {
//                    whereConditions.Add("r.Status = @Status");
//                    parameters.Add("@Status", (int)status.Value);
//                }

//                if (!string.IsNullOrEmpty(createdBy))
//                {
//                    whereConditions.Add("r.CreatedBy = @CreatedBy");
//                    parameters.Add("@CreatedBy", createdBy);
//                }

//                var whereClause = whereConditions.Count > 0
//                    ? $"WHERE {string.Join(" AND ", whereConditions)}"
//                    : string.Empty;

//                var sql = $@"
//                    SELECT 
//                        r.ReportID,
//                        r.Title,
//                        r.Description,
//                        d.Name AS DatabaseName,
//                        r.ReportType,
//                        r.Status,
//                        r.CreatedBy,
//                        r.CreatedAt,
//                        r.LastModifiedAt,
//                        (SELECT COUNT(*) FROM ReportSections rs WHERE rs.ReportID = r.ReportID) AS SectionCount
//                    FROM Reports r
//                    LEFT JOIN Databases d ON r.DatabaseID = d.DatabaseID
//                    {whereClause}
//                    ORDER BY r.LastModifiedAt DESC";

//                return await WithConnectionAsync(async conn =>
//                {
//                    var reports = await conn.QuerySafeAsync<ReportListItemDto>(sql, parameters);
//                    return reports.ToList();
//                });
//            }
//            catch (Exception)
//            {
//                throw;
//            }
//        }

//        /// <summary>
//        /// Gets a report by ID including all sections.
//        /// </summary>
//        /// <param name="reportId">The report ID.</param>
//        /// <returns>The report with sections or null if not found.</returns>
//        public async Task<Report> GetReportByIdAsync(int reportId)
//        {
//            try
//            {
//                const string reportSql = @"
//                    SELECT 
//                        r.*,
//                        d.Name AS DatabaseName
//                    FROM Reports r
//                    LEFT JOIN Databases d ON r.DatabaseID = d.DatabaseID
//                    WHERE r.ReportID = @ReportID";

//                return await WithConnectionAsync(async conn =>
//                {
//                    var report = await conn.QueryFirstOrDefaultSafeAsync<Report>(
//                        reportSql, new { ReportID = reportId });

//                    if (report != null)
//                    {
//                        // Load sections
//                        report.Sections = (await GetSectionsByReportIdAsync(reportId, conn)).ToList();
//                    }

//                    return report;
//                });
//            }
//            catch (Exception)
//            {
//                throw;
//            }
//        }

//        /// <summary>
//        /// Gets a report by ID without sections (lightweight).
//        /// </summary>
//        /// <param name="reportId">The report ID.</param>
//        /// <returns>The report without sections.</returns>
//        public async Task<Report> GetReportHeaderAsync(int reportId)
//        {
//            try
//            {
//                const string sql = @"
//                    SELECT 
//                        r.*,
//                        d.Name AS DatabaseName
//                    FROM Reports r
//                    LEFT JOIN Databases d ON r.DatabaseID = d.DatabaseID
//                    WHERE r.ReportID = @ReportID";

//                return await WithConnectionAsync(async conn =>
//                {
//                    return await conn.QueryFirstOrDefaultSafeAsync<Report>(
//                        sql, new { ReportID = reportId });
//                });
//            }
//            catch (Exception)
//            {
//                throw;
//            }
//        }

//        /// <summary>
//        /// Creates a new report.
//        /// </summary>
//        /// <param name="report">The report to create.</param>
//        /// <returns>The created report with ID.</returns>
//        public async Task<Report> CreateReportAsync(Report report)
//        {
//            try
//            {
//                const string sql = @"
//                    INSERT INTO Reports (
//                        Title, Description, DatabaseID, ReportType, Status,
//                        GeneratedPrompt, ExecutiveSummary, LLMProvider, Configuration,
//                        IsTemplate, IsPublic, CreatedBy, CreatedAt, LastModifiedBy, LastModifiedAt
//                    ) VALUES (
//                        @Title, @Description, @DatabaseID, @ReportType, @Status,
//                        @GeneratedPrompt, @ExecutiveSummary, @LLMProvider, @Configuration,
//                        @IsTemplate, @IsPublic, @CreatedBy, @CreatedAt, @LastModifiedBy, @LastModifiedAt
//                    );
//                    SELECT CAST(SCOPE_IDENTITY() as int)";

//                report.CreatedAt = DateTime.UtcNow;
//                report.LastModifiedAt = DateTime.UtcNow;
//                report.LastModifiedBy = report.CreatedBy;

//                return await WithConnectionAsync(async conn =>
//                {
//                    using var transaction = conn.BeginTransaction();
//                    try
//                    {
//                        // Insert report
//                        report.ReportID = await conn.ExecuteScalarSafeAsync<int>(sql, new
//                        {
//                            report.Title,
//                            report.Description,
//                            report.DatabaseID,
//                            ReportType = (int)report.ReportType,
//                            Status = (int)report.Status,
//                            report.GeneratedPrompt,
//                            report.ExecutiveSummary,
//                            report.LLMProvider,
//                            report.Configuration,
//                            report.IsTemplate,
//                            report.IsPublic,
//                            report.CreatedBy,
//                            report.CreatedAt,
//                            report.LastModifiedBy,
//                            report.LastModifiedAt
//                        }, transaction);

//                        // Insert sections if any
//                        if (report.Sections?.Any() == true)
//                        {
//                            foreach (var section in report.Sections)
//                            {
//                                section.ReportID = report.ReportID;
//                                await CreateSectionInternalAsync(section, conn, transaction);
//                            }
//                        }

//                        transaction.Commit();
//                        return report;
//                    }
//                    catch
//                    {
//                        transaction.Rollback();
//                        throw;
//                    }
//                });
//            }
//            catch (Exception)
//            {
//                throw;
//            }
//        }

//        /// <summary>
//        /// Updates an existing report.
//        /// </summary>
//        /// <param name="report">The report to update.</param>
//        /// <returns>The updated report.</returns>
//        public async Task<Report> UpdateReportAsync(Report report)
//        {
//            try
//            {
//                const string sql = @"
//                    UPDATE Reports SET
//                        Title = @Title,
//                        Description = @Description,
//                        ReportType = @ReportType,
//                        Status = @Status,
//                        ExecutiveSummary = @ExecutiveSummary,
//                        Configuration = @Configuration,
//                        IsTemplate = @IsTemplate,
//                        IsPublic = @IsPublic,
//                        LastModifiedBy = @LastModifiedBy,
//                        LastModifiedAt = @LastModifiedAt
//                    WHERE ReportID = @ReportID";

//                report.LastModifiedAt = DateTime.UtcNow;

//                return await WithConnectionAsync(async conn =>
//                {
//                    await conn.ExecuteSafeAsync(sql, new
//                    {
//                        report.ReportID,
//                        report.Title,
//                        report.Description,
//                        ReportType = (int)report.ReportType,
//                        Status = (int)report.Status,
//                        report.ExecutiveSummary,
//                        report.Configuration,
//                        report.IsTemplate,
//                        report.IsPublic,
//                        report.LastModifiedBy,
//                        report.LastModifiedAt
//                    });

//                    return report;
//                });
//            }
//            catch (Exception)
//            {
//                throw;
//            }
//        }

//        /// <summary>
//        /// Deletes a report and all its sections.
//        /// </summary>
//        /// <param name="reportId">The report ID to delete.</param>
//        /// <returns>True if deleted successfully.</returns>
//        public async Task<bool> DeleteReportAsync(int reportId)
//        {
//            try
//            {
//                // Sections are deleted via CASCADE
//                const string sql = "DELETE FROM Reports WHERE ReportID = @ReportID";

//                return await WithConnectionAsync(async conn =>
//                {
//                    var rowsAffected = await conn.ExecuteSafeAsync(sql, new { ReportID = reportId });
//                    return rowsAffected > 0;
//                });
//            }
//            catch (Exception)
//            {
//                throw;
//            }
//        }

//        /// <summary>
//        /// Checks if a report exists.
//        /// </summary>
//        /// <param name="reportId">The report ID.</param>
//        /// <returns>True if exists.</returns>
//        public async Task<bool> ReportExistsAsync(int reportId)
//        {
//            try
//            {
//                const string sql = "SELECT COUNT(1) FROM Reports WHERE ReportID = @ReportID";

//                return await WithConnectionAsync(async conn =>
//                {
//                    var count = await conn.ExecuteScalarSafeAsync<int>(sql, new { ReportID = reportId });
//                    return count > 0;
//                });
//            }
//            catch (Exception)
//            {
//                throw;
//            }
//        }

//        #endregion

//        #region Section CRUD Operations

//        /// <summary>
//        /// Gets all sections for a report.
//        /// </summary>
//        /// <param name="reportId">The report ID.</param>
//        /// <param name="connection">Optional connection to use.</param>
//        /// <param name="transaction">Optional transaction to use.</param>
//        /// <returns>List of sections ordered by DisplayOrder.</returns>
//        public async Task<List<ReportSection>> GetSectionsByReportIdAsync(
//            int reportId,
//            IDbConnection connection = null,
//            IDbTransaction transaction = null)
//        {
//            const string sql = @"
//                SELECT *
//                FROM ReportSections
//                WHERE ReportID = @ReportID
//                ORDER BY DisplayOrder ASC";

//            if (connection != null)
//            {
//                var sections = await connection.QuerySafeAsync<ReportSection>(
//                    sql, new { ReportID = reportId }, transaction);
//                return sections.ToList();
//            }
//            else
//            {
//                return await WithConnectionAsync(async conn =>
//                {
//                    var sections = await conn.QuerySafeAsync<ReportSection>(
//                        sql, new { ReportID = reportId });
//                    return sections.ToList();
//                });
//            }
//        }

//        /// <summary>
//        /// Gets a section by ID.
//        /// </summary>
//        /// <param name="sectionId">The section ID.</param>
//        /// <param name="connection">Optional connection to use.</param>
//        /// <param name="transaction">Optional transaction to use.</param>
//        /// <returns>The section or null if not found.</returns>
//        public async Task<ReportSection> GetSectionByIdAsync(
//            int sectionId,
//            IDbConnection connection = null,
//            IDbTransaction transaction = null)
//        {
//            const string sql = "SELECT * FROM ReportSections WHERE SectionID = @SectionID";

//            if (connection != null)
//            {
//                return await connection.QueryFirstOrDefaultSafeAsync<ReportSection>(
//                    sql, new { SectionID = sectionId }, transaction);
//            }
//            else
//            {
//                return await WithConnectionAsync(async conn =>
//                {
//                    return await conn.QueryFirstOrDefaultSafeAsync<ReportSection>(
//                        sql, new { SectionID = sectionId });
//                });
//            }
//        }

//        /// <summary>
//        /// Creates a new section.
//        /// </summary>
//        /// <param name="section">The section to create.</param>
//        /// <param name="connection">Optional connection to use.</param>
//        /// <param name="transaction">Optional transaction to use.</param>
//        /// <returns>The created section with ID.</returns>
//        public async Task<ReportSection> CreateSectionAsync(
//            ReportSection section,
//            IDbConnection connection = null,
//            IDbTransaction transaction = null)
//        {
//            if (connection != null)
//            {
//                return await CreateSectionInternalAsync(section, connection, transaction);
//            }
//            else
//            {
//                return await WithConnectionAsync(async conn =>
//                {
//                    return await CreateSectionInternalAsync(section, conn);
//                });
//            }
//        }

//        /// <summary>
//        /// Internal method to create a section with provided connection.
//        /// </summary>
//        private async Task<ReportSection> CreateSectionInternalAsync(
//            ReportSection section,
//            IDbConnection connection,
//            IDbTransaction transaction = null)
//        {
//            // Get next display order if not specified
//            if (section.DisplayOrder == 0)
//            {
//                section.DisplayOrder = await GetNextSectionOrderInternalAsync(section.ReportID, connection, transaction);
//            }

//            const string sql = @"
//                INSERT INTO ReportSections (
//                    ReportID, Title, Description, SectionType, DisplayOrder,
//                    IsVisible, IsExpanded, QueryText, QueryIntent, TextContent,
//                    ColumnConfiguration, VisualizationConfig, ChartType, IsDisplayedAsChart,
//                    CreatedAt, LastModifiedAt
//                ) VALUES (
//                    @ReportID, @Title, @Description, @SectionType, @DisplayOrder,
//                    @IsVisible, @IsExpanded, @QueryText, @QueryIntent, @TextContent,
//                    @ColumnConfiguration, @VisualizationConfig, @ChartType, @IsDisplayedAsChart,
//                    @CreatedAt, @LastModifiedAt
//                );
//                SELECT CAST(SCOPE_IDENTITY() as int)";

//            section.CreatedAt = DateTime.UtcNow;
//            section.LastModifiedAt = DateTime.UtcNow;

//            var parameters = new
//            {
//                section.ReportID,
//                section.Title,
//                section.Description,
//                SectionType = (int)section.SectionType,
//                section.DisplayOrder,
//                section.IsVisible,
//                section.IsExpanded,
//                section.QueryText,
//                section.QueryIntent,
//                section.TextContent,
//                section.ColumnConfiguration,
//                section.VisualizationConfig,
//                section.ChartType,
//                section.IsDisplayedAsChart,
//                section.CreatedAt,
//                section.LastModifiedAt
//            };

//            section.SectionID = await connection.ExecuteScalarSafeAsync<int>(sql, parameters, transaction);

//            // Update report's last modified timestamp
//            await UpdateReportTimestampInternalAsync(section.ReportID, connection, transaction);

//            return section;
//        }

//        /// <summary>
//        /// Creates multiple sections at once.
//        /// </summary>
//        /// <param name="sections">The sections to create.</param>
//        /// <returns>The created sections with IDs.</returns>
//        public async Task<List<ReportSection>> CreateSectionsAsync(List<ReportSection> sections)
//        {
//            if (sections == null || !sections.Any())
//                return new List<ReportSection>();

//            try
//            {
//                return await WithConnectionAsync(async conn =>
//                {
//                    using var transaction = conn.BeginTransaction();
//                    try
//                    {
//                        var createdSections = new List<ReportSection>();

//                        foreach (var section in sections)
//                        {
//                            var created = await CreateSectionInternalAsync(section, conn, transaction);
//                            createdSections.Add(created);
//                        }

//                        transaction.Commit();
//                        return createdSections;
//                    }
//                    catch
//                    {
//                        transaction.Rollback();
//                        throw;
//                    }
//                });
//            }
//            catch (Exception)
//            {
//                throw;
//            }
//        }

//        /// <summary>
//        /// Updates an existing section.
//        /// </summary>
//        /// <param name="section">The section to update.</param>
//        /// <param name="connection">Optional connection to use.</param>
//        /// <param name="transaction">Optional transaction to use.</param>
//        /// <returns>The updated section.</returns>
//        public async Task<ReportSection> UpdateSectionAsync(
//            ReportSection section,
//            IDbConnection connection = null,
//            IDbTransaction transaction = null)
//        {
//            const string sql = @"
//                UPDATE ReportSections SET
//                    Title = @Title,
//                    Description = @Description,
//                    SectionType = @SectionType,
//                    DisplayOrder = @DisplayOrder,
//                    IsVisible = @IsVisible,
//                    IsExpanded = @IsExpanded,
//                    QueryText = @QueryText,
//                    QueryIntent = @QueryIntent,
//                    TextContent = @TextContent,
//                    ColumnConfiguration = @ColumnConfiguration,
//                    VisualizationConfig = @VisualizationConfig,
//                    ChartType = @ChartType,
//                    IsDisplayedAsChart = @IsDisplayedAsChart,
//                    LastModifiedAt = @LastModifiedAt
//                WHERE SectionID = @SectionID";

//            section.LastModifiedAt = DateTime.UtcNow;

//            var parameters = new
//            {
//                section.SectionID,
//                section.Title,
//                section.Description,
//                SectionType = (int)section.SectionType,
//                section.DisplayOrder,
//                section.IsVisible,
//                section.IsExpanded,
//                section.QueryText,
//                section.QueryIntent,
//                section.TextContent,
//                section.ColumnConfiguration,
//                section.VisualizationConfig,
//                section.ChartType,
//                section.IsDisplayedAsChart,
//                section.LastModifiedAt
//            };

//            if (connection != null)
//            {
//                await connection.ExecuteSafeAsync(sql, parameters, transaction);
//                await UpdateReportTimestampInternalAsync(section.ReportID, connection, transaction);
//                return section;
//            }
//            else
//            {
//                return await WithConnectionAsync(async conn =>
//                {
//                    await conn.ExecuteSafeAsync(sql, parameters);
//                    await UpdateReportTimestampInternalAsync(section.ReportID, conn);
//                    return section;
//                });
//            }
//        }

//        /// <summary>
//        /// Deletes a section.
//        /// </summary>
//        /// <param name="sectionId">The section ID to delete.</param>
//        /// <param name="connection">Optional connection to use.</param>
//        /// <param name="transaction">Optional transaction to use.</param>
//        /// <returns>True if deleted successfully.</returns>
//        public async Task<bool> DeleteSectionAsync(
//            int sectionId,
//            IDbConnection connection = null,
//            IDbTransaction transaction = null)
//        {
//            const string sql = "DELETE FROM ReportSections WHERE SectionID = @SectionID";

//            if (connection != null)
//            {
//                // Get section info first for timestamp update
//                var section = await GetSectionByIdAsync(sectionId, connection, transaction);
//                if (section == null)
//                    return false;

//                var result = await connection.ExecuteSafeAsync(sql, new { SectionID = sectionId }, transaction);

//                if (result > 0)
//                {
//                    await UpdateReportTimestampInternalAsync(section.ReportID, connection, transaction);
//                    await ReorderSectionsAfterDeleteInternalAsync(section.ReportID, connection, transaction);
//                }

//                return result > 0;
//            }
//            else
//            {
//                return await WithConnectionAsync(async conn =>
//                {
//                    // Get section info first for timestamp update
//                    var section = await GetSectionByIdAsync(sectionId, conn);
//                    if (section == null)
//                        return false;

//                    var result = await conn.ExecuteSafeAsync(sql, new { SectionID = sectionId });

//                    if (result > 0)
//                    {
//                        await UpdateReportTimestampInternalAsync(section.ReportID, conn);
//                        await ReorderSectionsAfterDeleteInternalAsync(section.ReportID, conn);
//                    }

//                    return result > 0;
//                });
//            }
//        }

//        /// <summary>
//        /// Reorders sections within a report.
//        /// </summary>
//        /// <param name="reportId">The report ID.</param>
//        /// <param name="sectionOrder">List of section IDs in new order.</param>
//        /// <returns>True if reordered successfully.</returns>
//        public async Task<bool> ReorderSectionsAsync(int reportId, List<int> sectionOrder)
//        {
//            try
//            {
//                return await WithConnectionAsync(async conn =>
//                {
//                    using var transaction = conn.BeginTransaction();
//                    try
//                    {
//                        for (int i = 0; i < sectionOrder.Count; i++)
//                        {
//                            const string sql = @"
//                                UPDATE ReportSections 
//                                SET DisplayOrder = @DisplayOrder, LastModifiedAt = @LastModifiedAt
//                                WHERE SectionID = @SectionID AND ReportID = @ReportID";

//                            await conn.ExecuteSafeAsync(sql, new
//                            {
//                                DisplayOrder = i,
//                                LastModifiedAt = DateTime.UtcNow,
//                                SectionID = sectionOrder[i],
//                                ReportID = reportId
//                            }, transaction);
//                        }

//                        // Update report timestamp
//                        await UpdateReportTimestampInternalAsync(reportId, conn, transaction);

//                        transaction.Commit();
//                        return true;
//                    }
//                    catch
//                    {
//                        transaction.Rollback();
//                        throw;
//                    }
//                });
//            }
//            catch (Exception)
//            {
//                throw;
//            }
//        }

//        /// <summary>
//        /// Gets the next display order for a new section.
//        /// </summary>
//        /// <param name="reportId">The report ID.</param>
//        /// <param name="connection">Optional connection to use.</param>
//        /// <param name="transaction">Optional transaction to use.</param>
//        /// <returns>The next display order value.</returns>
//        public async Task<int> GetNextSectionOrderAsync(
//            int reportId,
//            IDbConnection connection = null,
//            IDbTransaction transaction = null)
//        {
//            if (connection != null)
//            {
//                return await GetNextSectionOrderInternalAsync(reportId, connection, transaction);
//            }
//            else
//            {
//                return await WithConnectionAsync(async conn =>
//                {
//                    return await GetNextSectionOrderInternalAsync(reportId, conn);
//                });
//            }
//        }

//        /// <summary>
//        /// Internal method to get next section order.
//        /// </summary>
//        private async Task<int> GetNextSectionOrderInternalAsync(
//            int reportId,
//            IDbConnection connection,
//            IDbTransaction transaction = null)
//        {
//            const string sql = @"
//                SELECT ISNULL(MAX(DisplayOrder), -1) + 1 
//                FROM ReportSections 
//                WHERE ReportID = @ReportID";

//            return await connection.ExecuteScalarSafeAsync<int>(sql, new { ReportID = reportId }, transaction);
//        }

//        #endregion

//        #region Query Execution

//        /// <summary>
//        /// Executes a SQL query on the specified database.
//        /// </summary>
//        /// <param name="sql">The SQL query to execute.</param>
//        /// <param name="databaseId">The database ID.</param>
//        /// <returns>Query results as list of dictionaries.</returns>
//        public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string sql, int databaseId)
//        {
//            try
//            {
//                var connectionString = await GetDatabaseConnectionStringAsync(databaseId);
//                if (string.IsNullOrEmpty(connectionString))
//                    throw new InvalidOperationException($"Database with ID {databaseId} not found or has no connection string.");

//                return await WithExternalConnectionAsync(connectionString, async conn =>
//                {
//                    var results = new List<Dictionary<string, object>>();

//                    using var reader = await conn.ExecuteReaderAsync(sql);
//                    while (await reader.ReadAsync())
//                    {
//                        var row = new Dictionary<string, object>();
//                        for (int i = 0; i < reader.FieldCount; i++)
//                        {
//                            var value = reader.GetValue(i);
//                            row[reader.GetName(i)] = value == DBNull.Value ? null : value;
//                        }
//                        results.Add(row);
//                    }

//                    return results;
//                });
//            }
//            catch (Exception)
//            {
//                throw;
//            }
//        }

//        /// <summary>
//        /// Executes a SQL query with pagination.
//        /// </summary>
//        /// <param name="sql">The SQL query.</param>
//        /// <param name="databaseId">The database ID.</param>
//        /// <param name="page">Page number (1-based).</param>
//        /// <param name="pageSize">Page size.</param>
//        /// <param name="sortColumn">Optional sort column.</param>
//        /// <param name="sortDirection">Sort direction (asc/desc).</param>
//        /// <returns>Paginated query results.</returns>
//        public async Task<(List<Dictionary<string, object>> Data, int TotalCount)> ExecuteQueryWithPaginationAsync(
//            string sql,
//            int databaseId,
//            int page = 1,
//            int pageSize = 25,
//            string sortColumn = null,
//            string sortDirection = "asc")
//        {
//            try
//            {
//                var connectionString = await GetDatabaseConnectionStringAsync(databaseId);
//                if (string.IsNullOrEmpty(connectionString))
//                    throw new InvalidOperationException($"Database with ID {databaseId} not found.");

//                return await WithExternalConnectionAsync(connectionString, async conn =>
//                {
//                    // Wrap original query for counting
//                    var countSql = $"SELECT COUNT(*) FROM ({sql}) AS CountQuery";
//                    var totalCount = await conn.ExecuteScalarSafeAsync<int>(countSql);

//                    // Build paginated query
//                    var offset = (page - 1) * pageSize;
//                    var paginatedSql = BuildPaginatedQuery(sql, sortColumn, sortDirection, offset, pageSize);

//                    var results = new List<Dictionary<string, object>>();

//                    using var reader = await conn.ExecuteReaderAsync(paginatedSql);
//                    while (await reader.ReadAsync())
//                    {
//                        var row = new Dictionary<string, object>();
//                        for (int i = 0; i < reader.FieldCount; i++)
//                        {
//                            var value = reader.GetValue(i);
//                            row[reader.GetName(i)] = value == DBNull.Value ? null : value;
//                        }
//                        results.Add(row);
//                    }

//                    return (results, totalCount);
//                });
//            }
//            catch (Exception)
//            {
//                throw;
//            }
//        }

//        /// <summary>
//        /// Validates a SQL query without executing it.
//        /// </summary>
//        /// <param name="sql">The SQL query to validate.</param>
//        /// <param name="databaseId">The database ID.</param>
//        /// <returns>Validation result with any error message.</returns>
//        public async Task<(bool IsValid, string ErrorMessage)> ValidateQueryAsync(string sql, int databaseId)
//        {
//            try
//            {
//                var connectionString = await GetDatabaseConnectionStringAsync(databaseId);
//                if (string.IsNullOrEmpty(connectionString))
//                    return (false, $"Database with ID {databaseId} not found.");

//                return await WithExternalConnectionAsync(connectionString, async conn =>
//                {
//                    try
//                    {
//                        // Use SET FMTONLY to validate without executing
//                        var validateSql = $"SET FMTONLY ON; {sql}; SET FMTONLY OFF;";
//                        await conn.ExecuteSafeAsync(validateSql);
//                        return (true, (string)null);
//                    }
//                    catch (Exception ex)
//                    {
//                        return (false, ex.Message);
//                    }
//                });
//            }
//            catch (Exception ex)
//            {
//                return (false, $"Validation error: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// Gets column metadata from a query result.
//        /// </summary>
//        /// <param name="sql">The SQL query.</param>
//        /// <param name="databaseId">The database ID.</param>
//        /// <returns>List of column metadata.</returns>
//        public async Task<List<ColumnMetadata>> GetQueryColumnsAsync(string sql, int databaseId)
//        {
//            try
//            {
//                var connectionString = await GetDatabaseConnectionStringAsync(databaseId);
//                if (string.IsNullOrEmpty(connectionString))
//                    throw new InvalidOperationException($"Database with ID {databaseId} not found.");

//                return await WithExternalConnectionAsync(connectionString, async conn =>
//                {
//                    // Get schema only without fetching data
//                    var schemaOnlySql = $"SELECT TOP 0 * FROM ({sql}) AS SchemaQuery";
//                    var columns = new List<ColumnMetadata>();

//                    using var reader = await conn.ExecuteReaderAsync(schemaOnlySql, commandType: CommandType.Text);
//                    var schemaTable = reader.GetSchemaTable();

//                    if (schemaTable != null)
//                    {
//                        foreach (DataRow row in schemaTable.Rows)
//                        {
//                            columns.Add(new ColumnMetadata
//                            {
//                                Name = row["ColumnName"].ToString(),
//                                DataType = row["DataType"].ToString(),
//                                IsNullable = (bool)row["AllowDBNull"]
//                            });
//                        }
//                    }

//                    return columns;
//                });
//            }
//            catch (Exception)
//            {
//                throw;
//            }
//        }

//        #endregion

//        #region Export History

//        /// <summary>
//        /// Records an export operation.
//        /// </summary>
//        /// <param name="reportId">The report ID.</param>
//        /// <param name="format">The export format.</param>
//        /// <param name="exportedBy">User who exported.</param>
//        /// <param name="fileSizeBytes">Size of exported file.</param>
//        /// <param name="success">Whether export succeeded.</param>
//        /// <param name="errorMessage">Error message if failed.</param>
//        public async Task RecordExportAsync(
//            int reportId,
//            ReportExportFormat format,
//            string exportedBy,
//            long? fileSizeBytes,
//            bool success,
//            string errorMessage = null)
//        {
//            try
//            {
//                const string sql = @"
//                    INSERT INTO ReportExportHistory (
//                        ReportID, ExportFormat, ExportedBy, ExportedAt, 
//                        FileSizeBytes, Success, ErrorMessage
//                    ) VALUES (
//                        @ReportID, @ExportFormat, @ExportedBy, @ExportedAt,
//                        @FileSizeBytes, @Success, @ErrorMessage
//                    )";

//                await WithConnectionAsync(async conn =>
//                {
//                    await conn.ExecuteSafeAsync(sql, new
//                    {
//                        ReportID = reportId,
//                        ExportFormat = (int)format,
//                        ExportedBy = exportedBy,
//                        ExportedAt = DateTime.UtcNow,
//                        FileSizeBytes = fileSizeBytes,
//                        Success = success,
//                        ErrorMessage = errorMessage
//                    });
//                    return true;
//                });
//            }
//            catch (Exception)
//            {
//                throw;
//            }
//        }

//        #endregion

//        #region Private Helper Methods

//        /// <summary>
//        /// Gets the connection string for a specific database.
//        /// </summary>
//        /// <param name="databaseId">The database ID.</param>
//        /// <param name="connection">Optional connection to use.</param>
//        /// <param name="transaction">Optional transaction to use.</param>
//        /// <returns>The connection string.</returns>
//        private async Task<string> GetDatabaseConnectionStringAsync(
//            int databaseId,
//            IDbConnection connection = null,
//            IDbTransaction transaction = null)
//        {
//            const string sql = "SELECT ConnectionString FROM Databases WHERE DatabaseID = @DatabaseID";

//            if (connection != null)
//            {
//                return await connection.QueryFirstOrDefaultSafeAsync<string>(
//                    sql, new { DatabaseID = databaseId }, transaction);
//            }
//            else
//            {
//                return await WithConnectionAsync(async conn =>
//                {
//                    return await conn.QueryFirstOrDefaultSafeAsync<string>(
//                        sql, new { DatabaseID = databaseId });
//                });
//            }
//        }

//        /// <summary>
//        /// Updates the report's last modified timestamp.
//        /// </summary>
//        private async Task UpdateReportTimestampInternalAsync(
//            int reportId,
//            IDbConnection connection,
//            IDbTransaction transaction = null)
//        {
//            const string sql = "UPDATE Reports SET LastModifiedAt = @LastModifiedAt WHERE ReportID = @ReportID";
//            await connection.ExecuteSafeAsync(sql,
//                new { LastModifiedAt = DateTime.UtcNow, ReportID = reportId }, transaction);
//        }

//        /// <summary>
//        /// Reorders sections after a delete to ensure sequential order.
//        /// </summary>
//        private async Task ReorderSectionsAfterDeleteInternalAsync(
//            int reportId,
//            IDbConnection connection,
//            IDbTransaction transaction = null)
//        {
//            const string sql = @"
//                ;WITH OrderedSections AS (
//                    SELECT SectionID, ROW_NUMBER() OVER (ORDER BY DisplayOrder) - 1 AS NewOrder
//                    FROM ReportSections
//                    WHERE ReportID = @ReportID
//                )
//                UPDATE rs
//                SET rs.DisplayOrder = os.NewOrder
//                FROM ReportSections rs
//                INNER JOIN OrderedSections os ON rs.SectionID = os.SectionID";

//            await connection.ExecuteSafeAsync(sql, new { ReportID = reportId }, transaction);
//        }

//        /// <summary>
//        /// Executes an action with an external database connection.
//        /// </summary>
//        /// <typeparam name="T">The return type.</typeparam>
//        /// <param name="connectionString">The external connection string.</param>
//        /// <param name="action">The action to execute.</param>
//        /// <returns>The result of the action.</returns>
//        private async Task<T> WithExternalConnectionAsync<T>(string connectionString, Func<IDbConnection, Task<T>> action)
//        {
//            using var connection = ConnectionFactory.CreateConnection(connectionString);
//            if (connection.State != ConnectionState.Open)
//                connection.Open();

//            return await action(connection);
//        }

//        /// <summary>
//        /// Builds a paginated query with sorting.
//        /// </summary>
//        /// <param name="sql">The original SQL query.</param>
//        /// <param name="sortColumn">The column to sort by.</param>
//        /// <param name="sortDirection">The sort direction.</param>
//        /// <param name="offset">The offset for pagination.</param>
//        /// <param name="pageSize">The page size.</param>
//        /// <returns>The paginated SQL query.</returns>
//        private string BuildPaginatedQuery(string sql, string sortColumn, string sortDirection, int offset, int pageSize)
//        {
//            var orderBy = string.IsNullOrEmpty(sortColumn)
//                ? "ORDER BY (SELECT NULL)"
//                : $"ORDER BY [{sortColumn}] {(sortDirection?.ToLower() == "desc" ? "DESC" : "ASC")}";

//            return $@"
//                SELECT * FROM (
//                    {sql}
//                ) AS PaginatedQuery
//                {orderBy}
//                OFFSET {offset} ROWS
//                FETCH NEXT {pageSize} ROWS ONLY";
//        }

//        #endregion
//    }
//}
