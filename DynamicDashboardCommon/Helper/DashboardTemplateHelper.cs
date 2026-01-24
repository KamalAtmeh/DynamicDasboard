using DynamicDashboardCommon.Models;
using System.Text.Json;

namespace DynamicDashboardCommon.Helper
{
    /// <summary>
    /// Helper class for loading and managing dashboard templates.
    /// Templates define layout (positions) while LLM generates content (titles, queries, chart types).
    /// </summary>
    public static class DashboardTemplateHelper
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Gets a template by its ID from the templates file.
        /// </summary>
        /// <param name="templateId">The template ID to find</param>
        /// <param name="templatesFilePath">Path to the templates JSON file</param>
        /// <returns>The template if found, null otherwise</returns>
        public static DashboardTemplate GetTemplateById(string templateId, string templatesFilePath)
        {
            try
            {
                var templates = LoadTemplates(templatesFilePath);
                return templates?.FirstOrDefault(t =>
                    string.Equals(t.Id, templateId, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Gets all available templates from the templates file.
        /// </summary>
        /// <param name="templatesFilePath">Path to the templates JSON file</param>
        /// <returns>List of all templates</returns>
        public static List<DashboardTemplate> GetAllTemplates(string templatesFilePath)
        {
            try
            {
                return LoadTemplates(templatesFilePath) ?? new List<DashboardTemplate>();
            }
            catch (Exception)
            {
                return new List<DashboardTemplate>();
            }
        }

        /// <summary>
        /// Gets templates filtered by category.
        /// </summary>
        /// <param name="category">The category to filter by</param>
        /// <param name="templatesFilePath">Path to the templates JSON file</param>
        /// <returns>List of templates in the specified category</returns>
        public static List<DashboardTemplate> GetTemplatesByCategory(string category, string templatesFilePath)
        {
            try
            {
                var templates = LoadTemplates(templatesFilePath);
                if (templates == null) return new List<DashboardTemplate>();

                return templates
                    .Where(t => string.Equals(t.Category, category, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            catch (Exception)
            {
                return new List<DashboardTemplate>();
            }
        }

        /// <summary>
        /// Gets all unique template categories.
        /// </summary>
        /// <param name="templatesFilePath">Path to the templates JSON file</param>
        /// <returns>List of unique category names</returns>
        public static List<string> GetTemplateCategories(string templatesFilePath)
        {
            try
            {
                var templates = LoadTemplates(templatesFilePath);
                if (templates == null) return new List<string>();

                return templates
                    .Select(t => t.Category)
                    .Where(c => !string.IsNullOrEmpty(c))
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Gets template summary information for UI display.
        /// </summary>
        /// <param name="templatesFilePath">Path to the templates JSON file</param>
        /// <returns>List of template summaries</returns>
        public static List<TemplateSummary> GetTemplateSummaries(string templatesFilePath)
        {
            try
            {
                var templates = LoadTemplates(templatesFilePath);
                if (templates == null) return new List<TemplateSummary>();

                return templates.Select(t => new TemplateSummary
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description,
                    Category = t.Category,
                    ThumbnailUrl = t.ThumbnailUrl,
                    KpiCount = t.Components?.Count(c => c.Type.Equals("kpi", StringComparison.OrdinalIgnoreCase)) ?? 0,
                    ChartCount = t.Components?.Count(c => c.Type.Equals("chart", StringComparison.OrdinalIgnoreCase)) ?? 0,
                    TableCount = t.Components?.Count(c => c.Type.Equals("table", StringComparison.OrdinalIgnoreCase)) ?? 0,
                    TotalComponents = t.Components?.Count ?? 0,
                    TargetAudience = t.AIGuidance?.TargetAudience
                }).ToList();
            }
            catch (Exception)
            {
                return new List<TemplateSummary>();
            }
        }

        /// <summary>
        /// Loads templates from the JSON file.
        /// </summary>
        private static List<DashboardTemplate> LoadTemplates(string templatesFilePath)
        {
            if (string.IsNullOrEmpty(templatesFilePath))
                return null;

            if (!File.Exists(templatesFilePath))
                return null;

            string json = File.ReadAllText(templatesFilePath);

            var wrapper = JsonSerializer.Deserialize<TemplatesWrapper>(json, _jsonOptions);
            return wrapper?.Templates;
        }

        /// <summary>
        /// Wrapper class for deserializing the templates JSON file.
        /// </summary>
        private class TemplatesWrapper
        {
            public List<DashboardTemplate> Templates { get; set; }
        }
    }

    /// <summary>
    /// Summary information for a template (for UI display).
    /// </summary>
    public class TemplateSummary
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string ThumbnailUrl { get; set; }
        public int KpiCount { get; set; }
        public int ChartCount { get; set; }
        public int TableCount { get; set; }
        public int TotalComponents { get; set; }
        public string TargetAudience { get; set; }
    }
}